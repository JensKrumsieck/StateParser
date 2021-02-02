using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StateParser
{
    internal class Program
    {
        private const string StartBlockTddft = "ORCA TD-DFT/TDA CALCULATION";
        private const string StartBlockStates = "EXCITED STATES";
        private const string EndBlockTddft = " *** ORCA-CIS/TD-DFT FINISHED WITHOUT ERROR ***";
        private const string EndBlockStates = "CALCULATED SOLVENT SHIFTS";

        private static void Main(string[] args)
        {
            var file = HandleInput(args);
            var block = File.ReadAllLines(file)
                .SkipWhile(s => !s.Contains(StartBlockTddft))
                .TakeWhile(s => !s.Contains(EndBlockTddft)).ToList();
            var stateBlock = string.Join("\n", block
                .SkipWhile(s => !s.Contains(StartBlockStates))
                .TakeWhile(s => !s.Contains(EndBlockStates)));
            var states = stateBlock.Split("STATE", StringSplitOptions.RemoveEmptyEntries)
                .Select(s => "STATE " + s)
                .SkipWhile(s => !Regex.IsMatch(s, "^STATE *\\d+.*(\\n.*)*"))
                .ToArray();
            Console.WriteLine($"Found {states.Length} States in file {file}");
            var stateList = new List<State>();
            foreach (var state in states)
            {
                var stateNo = Regex.Match(state, "STATE *(\\d+)").Groups[1].Value;
                Console.WriteLine($"Working on STATE {stateNo}");
                int.TryParse(stateNo, out var id);
                var transitionPattern = @"(\d*[a,b]) -> (\d*[a,b])[ ,:.]*(\d*[,.]?\d*)";
                var transitions = Regex.Matches(state, transitionPattern);
                var transitionList = transitions.Select(s => (s.Groups[1].Value, s.Groups[2].Value,
                    Convert.ToDouble(s.Groups[3].Value, CultureInfo.InvariantCulture))).ToList();
                var sum = transitionList.Sum(s => s.Item3);
                stateList.Add(new State(id, transitionList, sum));
            }

            var csvString = "";
            foreach (var state in stateList)
            {
                Console.WriteLine($"Writing STATE {state.id} to file");
                csvString += $"STATE {state.id};\n";
                foreach (var transition in state.transitions)
                {
                    csvString +=
                        $"{transition.Item1};{transition.Item2};{transition.Item3};{transition.Item3 / state.sum};\n";
                }
            }
            File.WriteAllText(file + ".csv",csvString);
        }

        private static string HandleInput(string[] args)
        {
            string path;
            if (args.Length == 0)
            {
                Console.WriteLine("Enter Path of ORCA .out file:");
                path = Console.ReadLine();
            }
            else path = args[0];
            if (!File.Exists(path)) Environment.Exit(1);
            return path;
        }

        internal record State(int id, List<(string, string, double)> transitions, double sum);
    }
}
