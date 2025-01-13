using System;
using System.Linq;
using System.Collections.Generic;
namespace ErrorHandling 
{
    public class Checker 
    {
            List<CompilerError> Errors = [];
            public const byte REG = 0b0001;
            public const byte POSINT = 0b0010;
            public const byte NEGINT = 0b0100;
            public const byte FLOAT = 0b1000;
            const byte NULL = 0;
            const byte INT = POSINT | NEGINT;
            const byte NUM = INT|FLOAT;

            Dictionary<string,byte[]> BitCheck = new Dictionary<string,byte[]>() 
            {
                {"ADD", new byte[] {REG,REG|INT,REG|INT}},
                {"SUB", new byte[] {REG,REG|INT,REG|INT}},
                {"CMP", new byte[] {REG|NUM,REG|NUM}}, //!
                {"MOV", new byte[] {REG,REG|NUM}},
                {"HALT", new byte[] {NULL}},
            };

            static string BitSwitch(byte num) => num switch 
            {
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
            public bool success = true;

            static ushort BitAssemble(byte[] inByte) 
            {
                ushort outByte = 0;
                const byte NIBBLE = 4;
                byte shift = 3;
                foreach (byte num in inByte) 
                {
                    outByte ^= (ushort)(num << NIBBLE * shift);
                    --shift;
                }
                return outByte;
            }

            public bool FuncFormatChecker(int line_number, string line, string opcode, Assembling.Operand[] operands) 
            {
                success = false;
                const ushort NIBBLE_MASK = 0xF000;
                
                ushort control = BitAssemble(BitCheck[opcode]);
                ushort check = BitAssemble(operands.Select(operand => operand.Use).ToArray());
                ushort test = (ushort)(control & check);

                if (test == check) 
                {
                    return true;
                }

                for (int shift = 0; shift < 16; shift += 4) 
                {
                    ushort mask = (ushort)(NIBBLE_MASK >> shift);
                    byte testByte = (byte)((test & mask) >> (12 - shift));
                    byte controlByte = (byte)((control & mask) >> (12 - shift));
                    byte checkByte = (byte)((check & mask) >> (12 - shift));
                    if (testByte == 0 && controlByte != checkByte) 
                    {
                        string actual = BitSwitch(controlByte);
                        string written = BitSwitch(checkByte);
                        Errors.Add(new CompilerError(line_number, line, $"ARG [{shift/4}] was {written} but {opcode} takes {actual}", ErrType.InvalidArguement));
                    }
                }
                return false;
            }  


            public IEnumerable<string> Display() 
            {
                return Errors.Select(error => $"{error.Type} Error found at line {error.lineNumber}: {error.Line}\n{error.Context}");
            }    
        }

    struct CompilerError 
    {
        public readonly string Line;
        public readonly string Context;
        public readonly int lineNumber;
        public ErrType Type;

        public CompilerError(int lineNumber, string line, string context, ErrType type) 
        {
            this.Context = context;
            this.Line = line;
            this.lineNumber = lineNumber;
            this.Type = type;
        }
    }

    enum ErrType 
    {
        Syntax,
        InvalidArguement,
    }
}