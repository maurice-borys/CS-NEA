using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Compiler;
using OneOf;

class Simulation
{
    static void Simulate(string[] args) 
    {
        IEnumerable<string> lines = File.ReadLines("INPUT.txt");
        Compiler.Compiler aqa_compiler = new Compiler.Compiler(new Checker());
        Action<State>[]? commands = aqa_compiler.Parse(lines);
        if (commands == null) {
            foreach (var err in aqa_compiler.Check.Display()) {
                Console.WriteLine(err);
            }
            return;
        }

        State state = new State();
        while (state.GetPC() < commands.Count()) {
            Action<State> next = commands[state.GetPC()];
            next(state);
            state.IncrPC();
        }
        state.Display();
    }
}

public enum CMP {
    EQ,
    NE, // only for jump 
    LT,
    GT,
    NULL,
}


public class State {
    const short SIGN_FLAG = 0b1000;
    const short ZERO_FLAG = 0b0100;
    const short CARRY_FLAG = 0b0010;
    const short OVERFLOW_FLAG = 0b0001;
    //short status;

    CMP Cmp_register;

    uint pc;
    OneOf<int,float>[] stack; 
    OneOf<int,float>[] registers;

    public State() {
        Cmp_register = CMP.NULL;
        pc = 1; //skip wait at 0
        registers = new OneOf<int, float>[16];
        stack = new OneOf<int, float>[1024];
    }

    public CMP GetCmp() {
        return Cmp_register;
    }

    public void SetCMP(CMP cmp) {
        Cmp_register = cmp;
    }

    public uint GetPC() {
        return pc;
    }

    public void SetPC(uint i) {
        pc = i;
    }

    public void IncrPC() {
        ++pc;
    }

    public void Halt() {
        pc = 2147483647;
    }

    public void AssignRegister(OneOf<int,float> reg_outdex, OneOf<int,float> value) {
        registers[reg_outdex.AsT0] = value;
    }

    public void Display() {
        foreach (var reg in registers) {
            Console.WriteLine(reg);
        }
    }

    public OneOf<int,float> GetValue(Operand operand) => operand.Use switch {
        Checker.REG => registers[operand.Value.AsT0],
        _ => operand.Value,
    };
}