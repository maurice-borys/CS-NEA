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
        public Checker Check;

        public Compiler(Checker check) {
            Check = check;
        }

        public Action<State>[]? Parse(IEnumerable<string> source) {
            IEnumerable<string> source_code = PreProcess(source);

            uint id = 0; 
            List<uint> ids = [];
            Dictionary<uint, Action<State>> actionIDs = [];
            List<Jump> jumps = [];
            Dictionary<string,uint> labels = [];
            // PREPROCESS TO REMOVE COMMENTS

            foreach (var (line_number,line) in source_code.Select((line,index) => (index, line))) {
                ++id;
                string[] tokens = Tokeniser.Matches(line.ToUpper()).Select(match => match.Groups[1].Value).ToArray();
                string opcode = tokens[0];
                string[] arguements = tokens[1..];
                switch (opcode) {
                    case string name when name.Last() == ':':
                        labels.Add(opcode[..(opcode.Length-1)],id);
                        continue;
                    case "B":
                        jumps.Add(new Jump(arguements,id));
                        ids.Add(id);
                        continue;
                }

                Operand[] operands = arguements.Select(ParseArgs).ToArray();
                Func<Operand[], Action<State>> func = OpDex[opcode];

                //type check
                if (!Check.FuncFormatChecker(line_number,line,opcode,operands)) {
                    continue;
                }
                //Console.WriteLine($"{Convert.ToString(control,2)} & {Convert.ToString(check,2)} = {Convert.ToString(test,2)}");
                

                actionIDs.Add(id, func(operands));
                ids.Add(id);                    
                }
                Action<State>[] commands = Linker(ids,actionIDs,jumps,labels);    
                if (Check.Success) {
                    return commands;
                }
                

                return null;
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
            '#' => new Operand(int.Parse(arg[1..])),
            'R' => new Operand(int.Parse(arg[1..]),'r'),
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


    public struct Operand {
        public byte Use;
        public OneOf<int,float> Value;

        public Operand(int val, char _) {
            Use = Checker.REG;
            Value = val;
        }

        public Operand(int val) {
            if (val >= 0) {
                Use = Checker.POSINT;
            } else {
                Use = Checker.NEGINT;
            }
            Value = val;
        }

        public Operand(float val) {
            Use = Checker.FLOAT;
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

    public class Checker {
        List<CompilerError> Errs;
        public const byte REG = 0b0001;
        public const byte POSINT = 0b0010;
        public const byte NEGINT = 0b0100;
        public const byte FLOAT = 0b1000;
        const byte NULL = 0;
        const byte INT = POSINT | NEGINT;
        const byte NUM = INT|FLOAT;

         Dictionary<string,byte[]> BitCheck = new Dictionary<string,byte[]>() {
            {"ADD", new byte[] {REG,REG|INT,REG|INT}},
            {"SUB", new byte[] {REG,REG|INT,REG|INT}},
            {"CMP", new byte[] {REG|NUM,REG|NUM}}, //!
            {"MOV", new byte[] {REG,REG|NUM}},
            {"HALT", new byte[] {NULL}},
        };

        static string BitSwitch(byte num) => num switch {
            0b00000001 => "REGISTER",
            0b00000010 => "POSITIVE INTEGER",
            0b00000100 => "NEGATIVE INTEGER",
            0b00001000 => "FLOAT",
            0b00000110 => "INT",
            0b00001110 => "NUMERIC",
            0b00001111 => "ANY",
            0 => "NONE",
            _ => throw new Exception("NOT FOUND"),
        };
        public bool Success;

        public Checker() {
            Errs = [];
            Success = true;
        }


         static ushort BitAssemble(byte[] in_byte) {
            ushort out_byte = 0;
            const byte NIBBLE = 4;
            byte shift = 3;
            foreach (byte num in in_byte) {
                out_byte ^= (ushort)(num << NIBBLE * shift);
                --shift;
            }
            return out_byte;
        }

        public bool FuncFormatChecker(int line_number, string line, string opcode, Operand[] operands) {
            Success = false;
            const ushort NIBBLE_MASK = 0xF000;
            
            ushort control = BitAssemble(BitCheck[opcode]);
            ushort check = BitAssemble(operands.Select(operand => operand.Use).ToArray());
            ushort test = (ushort)(control & check);

            if (test == check) {
                return true;
            }

            for (int shift = 0; shift < 16; shift += 4) {
                ushort mask = (ushort)(NIBBLE_MASK >> shift);
                byte testB = (byte)((test & mask) >> (12 - shift));
                byte ctrlB = (byte)((control & mask) >> (12 - shift));
                byte checkB = (byte)((check & mask) >> (12 - shift));
                if (testB == 0 && ctrlB != checkB) {
                    string actual = BitSwitch(ctrlB);
                    string written = BitSwitch(checkB);
                    Errs.Add(new CompilerError(line_number, line, $"ARG [{shift/4}] was {written} but {opcode} takes {actual}", ErrType.InvalidArguement));
                }
            }
            return false;
        }  


        public IEnumerable<string> Display() {
            return Errs.Select(error => $"{error.type} Error found at line {error.line_number}: {error.line}\n{error.context}");
        }    
    }

struct CompilerError {
        public string line;
        public string context;
        public int line_number;
        public ErrType type;

        public CompilerError(int line_number,string line,string context, ErrType type) {
            this.context = context;
            this.line = line;
            this.line_number = line_number;
            this.type = type;
        }
    }

    enum ErrType {
        Syntax,
        InvalidArguement,
    }
}