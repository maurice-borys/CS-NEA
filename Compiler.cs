using OneOf;
using System.Text.RegularExpressions;

namespace Compiler { 
    public class Compiler {
        const char BRANCH = 'B';
        const string REGISTER = "R[[0-9]|1[0-5]";
        const string LITERAL = "#\\d+";

        const string ARG = $"{REGISTER}|{LITERAL}";

        const string INTOP_FORMAT = $"({REGISTER})\\s*,\\s*({ARG})\\s*,\\s*({ARG})";
        const string TWO_FORMAT = $"({REGISTER})\\s*,\\s*({ARG})";
        const string BRANCH_NAME = "(\\w+):";
        Regex HaltFormat = new Regex("\\s*HALT\\s*",RegexOptions.IgnoreCase);
        Dictionary<string,Regex> Reggie = new Dictionary<string,Regex>() {
            {"ADD" , new Regex(INTOP_FORMAT,RegexOptions.IgnoreCase)},
            {"SUB" , new Regex(INTOP_FORMAT,RegexOptions.IgnoreCase)},
            {"XOR", new Regex(INTOP_FORMAT,RegexOptions.IgnoreCase)},
            {"CMP", new Regex(TWO_FORMAT,RegexOptions.IgnoreCase)},
            {"MOV", new Regex(TWO_FORMAT,RegexOptions.IgnoreCase)},
        };

        Dictionary<string,Func<Operand[], Action<State>>> OpDex = new Dictionary<string,Func<Operand[], Action<State>>>() {
            {"ADD", FuncAdd},
            {"SUB", FuncSub},
            {"CMP", FuncCMP},
            {"MOV", FuncMov}
        };

        
        public Action<State>[] Parse(string[] source_code) {
            uint id = 0; 
            List<uint> ids = [];
            Dictionary<uint, Action<State>> actionIDs = [];
            List<Jump> jumps = [];
            Dictionary<string,uint> labels = [];
            // PREPROCESS TO REMOVE COMMENTS
            foreach (string line in source_code) {
                string? start_line = string.Join("",line.SkipWhile((char chr) => chr == ' '));
                if (start_line == null) {
                    continue;
                }
                int splitdex = start_line.IndexOf(' ');
                if (splitdex == -1) {// Branch labels
                    Regex asfgd = new Regex(BRANCH_NAME,RegexOptions.IgnoreCase);
                    if (asfgd.IsMatch(line)) {
                        string name = asfgd.Match(line).Groups[1].Value;
                        labels.Add(name,id);
                        ++id;
                    } else if (HaltFormat.IsMatch(line)) {
                        actionIDs.Add(id, Halt);
                        ids.Add(id);
                        ++id;
                    }
                    continue;
                }

                string opcode = start_line[0..splitdex];
                string arguement = start_line[(splitdex+1)..];
                if (line[0] == BRANCH | opcode[0] == BRANCH) {
                    jumps.Add(new Jump(line,id));
                    ids.Add(id);
                    ++id;
                    continue;
                }
                Regex format_check = Reggie[opcode];
                if (format_check.IsMatch(arguement)) {
                    Operand[] parameters = format_check
                        .Match(arguement)
                        .Groups
                        .Cast<Group>()
                        .Skip(1)
                        .Select((Group arg) => ParseArgs(arg.Value))
                        .ToArray();
                    
                    Func<Operand[], Action<State>> func = OpDex[opcode];
                    actionIDs.Add(id, func(parameters));
                    ids.Add(id);
                    ++id;
                    
                }
            }
            return Linker(ids,actionIDs,jumps,labels);
        }    

        static Action<State> FuncJump(CMP condition, uint destination) {
            if (condition == CMP.NULL) {
                return state => state.SetPC(destination);
            }

            if (condition == CMP.NE) {
                return state => {
                    if (state.GetCmp() != CMP.EQ) {
                        state.SetPC(destination);
                    }
                };
            }
            
            return state => { //EQ|LT|GT
                if (condition == state.GetCmp()) {
                    state.SetPC(destination);
                }
            };

        }
        static Operand ParseArgs(string arg) {
            return arg[0] switch {
            '#' => new Operand(int.Parse(arg[1..]), Dtype.Literal),
            'R' => new Operand(int.Parse(arg[1..]), Dtype.Register),
            _ => throw new Exception("PAIN"),//!
            };
        }

        static Action<State>[] Linker(List<uint> ids, Dictionary<uint, Action<State>> actionIDs,List<Jump> jumps,Dictionary<string,uint> labels) {
            Dictionary<uint, Action<State>> jmps = jumps.ToDictionary( 
                jump => jump.GetID(),
                jump => FuncJump(jump.GetCondition(),labels[jump.GetName()])
            );
            
            uint i = 1;
            Action<State>[] commands = new Action<State>[ids.Count+1];
            commands[0] = Wait;
            foreach (uint id in ids) {
                if (actionIDs.ContainsKey(id)) {
                    commands[i] = actionIDs[id];
                    ++i;
                } else if (jmps.ContainsKey(id)) {
                    commands[i] = jmps[id];
                    ++i;
                }
            }

            return commands;
        }


        static Action<State> FuncAdd(Operand[] operands) {
            return state => {
                OneOf<int,float> result = state.GetValue(operands[1]).AsT0 + state.GetValue(operands[2]).AsT0;
                state.AssignRegister(operands[0].Value.AsT0, result);
            };
        }

        static Action<State> FuncSub(Operand[] operands) {
            return state => {
                OneOf<int,float> result = state.GetValue(operands[1]).AsT0 - state.GetValue(operands[2]).AsT0;
                state.AssignRegister(operands[0].Value, result);
            };
        }

        static Action<State> FuncCMP(Operand[] operands) {
            return state => {
                int x = state.GetValue(operands[0]).AsT0; 
                int y = state.GetValue(operands[1]).AsT0; 
                if (x > y) {
                    state.SetCMP(CMP.GT);
                } else if (x < y) {
                    state.SetCMP(CMP.LT);
                } else {
                    state.SetCMP(CMP.EQ);
                }
            };
        }

        static Action<State> FuncMov(Operand[] operands) {
            return state => {
                state.AssignRegister(operands[0].Value,state.GetValue(operands[1]));
            };
        }

        static void Halt(State state) {
                state.Halt();
        }

        static void Wait(State _) {
            return;
        }
    }    
}


public enum Dtype {
    Literal,
    Register,
}

public struct Operand {
    public Dtype Use;
    public OneOf<int,float> Value;

    public Operand(int val, Dtype type) {
        Use = type;
        Value = val;
    }

    public Operand(float val, Dtype type) {
        Use = type;
        Value = val;
    }
}

struct Jump {
    CMP condition;
    string name; 
    uint id;

    public Jump(string arguement, uint id) {
        this.id = id;
        Regex jump_format = new Regex("(EQ|NE|GT|LT)? (\\S+)");
        GroupCollection mas = jump_format.Match(arguement).Groups;
        name = mas[2].Value;

        string eq = mas[1].Value;
        condition = eq switch {
            "EQ" => CMP.EQ,
            "NE" => CMP.NE,
            "GT" => CMP.GT,
            "LT" => CMP.LT,
            _ => CMP.NULL,
        };
    }

    public string GetName() {
        return name;
    }

    public CMP GetCondition() {
        return condition;
    }

    public uint GetID() {
        return id;
    }
}