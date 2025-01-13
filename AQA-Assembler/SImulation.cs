using System;
using ErrorHandling;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assembling;
using OneOf;

class Simulation
{
    static void Simulate(string[] args) 
    {
        IEnumerable<string> lines = File.ReadLines("INPUT.txt");
        Assembling.Assembler aqAssembler = new Assembling.Assembler(new ErrorHandling.Checker());
        Action<State>[]? commands = aqAssembler.Parse(lines);
        if (commands == null) 
        {
            foreach (var err in aqAssembler.Check.Display()) 
            {
                Console.WriteLine(err);
            }
            return;
        }

        State state = new State();
        while (state.PC < commands.Count()) 
        {
            Action<State> next = commands[state.PC];
            next(state);
            state.IncrPC();
        }
        state.Display();
    }
}

public enum CMP 
{
    EQ,
    NE, // only for jump 
    LT,
    GT,
    NULL,
}


public class State 
{
    // const short SIGN_FLAG = 0b1000;
    // const short ZERO_FLAG = 0b0100;
    // const short CARRY_FLAG = 0b0010;
    // const short OVERFLOW_FLAG = 0b0001;

    public CMP CmpRegister {get;set;} = CMP.NULL;


    uint pc = 1;
    public uint PC 
    {
        get {return pc;}
        set {pc = value > 0 ? value : 1;}
    }

        
    OneOf<int,float>[] stack; 
    OneOf<int,float>[] registers;

    public State() 
    {
        registers = new OneOf<int, float>[16];
        stack = new OneOf<int, float>[1024];
    }

    public void IncrPC() 
    {
        ++pc;
    }

    public void Halt()
    {
        pc = uint.MaxValue;
    }

    public void AssignRegister(OneOf<int,float> reg_outdex, OneOf<int,float> value) 
    {
        registers[reg_outdex.AsT0] = value;
    }

    public void Display() 
    {
        foreach (var reg in registers) 
        {
            Console.WriteLine(reg);
        }
    }

    public OneOf<int,float> GetValue(Operand operand) => operand.Use switch 
    {
        Checker.REG => registers[operand.Value.AsT0],
        _ => operand.Value,
    };
}