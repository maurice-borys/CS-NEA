using OneOf;
using System.Text.RegularExpressions;

namespace Compiler { 
    public class Compiler {
        Dictionary<string,Func<Operand[], Action<State>>> OpDex = new Dictionary<string,Func<Operand[], Action<State>>>() {
            {"ADD", FuncAdd},
            {"SUB", FuncSub},
            {"CMP", FuncCMP},
            {"MOV", FuncMov},
            {"HALT", FuncHalt},
        };

        Regex Tokeniser = new Regex("(B|\\w+:|\\w+|#\\d+)");

        public Action<State>[] Parse(IEnumerable<string> source) {
            IEnumerable<string> source_code = PreProcess(source);
            uint id = 0; 
            List<uint> ids = [];
            Dictionary<uint, Action<State>> actionIDs = [];
            List<Jump> jumps = [];
            Dictionary<string,uint> labels = [];
            // PREPROCESS TO REMOVE COMMENTS
            foreach (string line in source_code) {
                string[] tokens = Tokeniser.Matches(line.ToUpper()).Select(match => match.Groups[1].Value).ToArray();
                string opcode = tokens[0];
                string[] arguements = tokens[1..];
                switch (opcode) {
                    case string name when name.Last() == ':':
                        labels.Add(opcode[..(opcode.Length-1)],id);
                        ++id;
                        continue;
                    case "B":
                        jumps.Add(new Jump(arguements,id));
                        ids.Add(id);
                        ++id;
                        continue;
                }

                Operand[] operands = arguements.Select(ParseArgs).ToArray();
                Func<Operand[], Action<State>> func = OpDex[opcode];
                actionIDs.Add(id, func(operands));
                ids.Add(id);
                ++id;
                    
                }
                return Linker(ids,actionIDs,jumps,labels);    
            }

        static IEnumerable<string> PreProcess(IEnumerable<string> source) {
            return source
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !string.IsNullOrEmpty(line));
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
                jump => FuncJump(jump.GetCondition(),labels[jump.GetLabel()])
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

        static Action<State> FuncHalt(Operand[] _) {
            return state => state.Halt();
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
    string label; 
    uint id;

    public Jump(string[] arguements, uint id) {
        this.id = id;
        condition = arguements[0] switch {
            "EQ" => CMP.EQ,
            "NE" => CMP.NE,
            "GT" => CMP.GT,
            "LT" => CMP.LT,
            _ => CMP.NULL,
        };
        if (condition == CMP.NULL) {
            label = arguements[0]; //B
        } else {
            label = arguements[1]; //B <cond> <label>
        }
    }

    public string GetLabel() {
        return label;
    }

    public CMP GetCondition() {
        return condition;
    }

    public uint GetID() {
        return id;
    }
}