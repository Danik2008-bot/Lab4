using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GeneticSearch
{
    class Program
    {
        // Один белок: название, организм, цепочка аминокислот (уже распакованная)
        struct Protein
        {
            public string name;         // название белка
            public string organism;     // организм
            public string amino_acids;  // цепочка аминокислот
        }

        // Одна команда из commands.txt: имя и до двух параметров
        struct Command
        {
            public string name;         // search / diff / mode
            public string parameter1;
            public string parameter2;   // заполнен только у diff
        }

        // ---------- Чтение файлов ----------

        static List<Command> ReadCommands(string filename)
        {
            List<Command> commands = new List<Command>();

            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.Trim() == "") continue;        // пустые строки пропускаем

                    string[] parts = line.Split('\t');

                    Command command;
                    command.name = parts[0];
                    command.parameter1 = parts[1];
                    command.parameter2 = parts.Length > 2 ? parts[2] : String.Empty;
                    commands.Add(command);
                }
            }
            return commands;
        }

        static List<Protein> ReadData(string filename)
        {
            List<Protein> data = new List<Protein>();

            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line.Trim() == "") continue;

                    string[] parts = line.Split('\t');

                    Protein protein;
                    protein.name = parts[0];
                    protein.organism = parts[1];
                    // цепочка может быть сжата RLE: распаковываем сразу при чтении
                    protein.amino_acids = RLDecoding(parts[2]);
                    data.Add(protein);
                }
            }
            return data;
        }

        // ---------- RLE ----------

        // Сжатие: AAAAAAAATA -> 8ATA (серии из 1 и 2 букв не сжимаем)
        static string RLEncoding(string amino_acids)
        {
            StringBuilder encoded = new StringBuilder();

            for (int i = 0; i < amino_acids.Length; i++)
            {
                char ch = amino_acids[i];
                int count = 1;

                // пока следующая буква такая же, увеличиваем счётчик серии
                while (i < amino_acids.Length - 1 && amino_acids[i + 1] == ch)
                {
                    count++;
                    i++;
                }

                if (count > 2) encoded.Append(count).Append(ch);
                else encoded.Append(ch, count);
            }
            return encoded.ToString();
        }

        // Распаковка: 8ATA -> AAAAAAAATA (цифра относится к букве справа)
        static string RLDecoding(string amino_acids)
        {
            StringBuilder decoded = new StringBuilder();

            for (int i = 0; i < amino_acids.Length; i++)
            {
                char ch = amino_acids[i];

                if (char.IsDigit(ch))
                {
                    int count = ch - '0';               // '8' -> 8
                    char letter = amino_acids[i + 1];   // повторяемая буква
                    decoded.Append(letter, count);
                    i++;                                // пропускаем эту букву
                }
                else
                {
                    decoded.Append(ch);
                }
            }
            return decoded.ToString();
        }

        static void Main(string[] args)
        {
            string sequencesFile = "sequences.0.txt";
            string commandsFile = "commands.0.txt";

            List<Protein> proteins = ReadData(sequencesFile);
            List<Command> commands = ReadCommands(commandsFile);

            Console.WriteLine("Белков: " + proteins.Count);
            Console.WriteLine("Команд: " + commands.Count);
        }
    }
}
