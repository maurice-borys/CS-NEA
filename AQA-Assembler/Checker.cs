using System;
using System.Linq;
using System.Collections.Generic;
namespace ErrorHandling {
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

            public bool FuncFormatChecker(int line_number, string line, string opcode, Assembling.Operand[] operands) {
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