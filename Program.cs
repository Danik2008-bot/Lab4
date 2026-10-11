using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GeneticSearch
{
    class Program
    {
        // Разделитель между операциями в выходном файле
        const string Line =
            "--------------------------------------------------------------------------";

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

        // ---------- Вспомогательные методы ----------

        // Номер операции в виде 001, 002, ...
        static string Number(int operationNumber)
        {
            return operationNumber.ToString("D3");
        }

        // Индекс белка с таким названием в списке или -1, если не найден
        static int FindProtein(List<Protein> proteins, string name)
        {
            for (int i = 0; i < proteins.Count; i++)
            {
                if (proteins[i].name == name) return i;
            }
            return -1;
        }

        // ---------- Операции ----------

        static void Search(List<Protein> proteins, string sequence,
                           int operationNumber, StreamWriter writer)
        {
            string wanted = RLDecoding(sequence);   // FK3I -> FKIII

            writer.WriteLine(Number(operationNumber) + "   search   " + wanted);
            writer.WriteLine("organism".PadRight(25) + "protein");

            bool found = false;
            for (int i = 0; i < proteins.Count; i++)
            {
                if (proteins[i].amino_acids.Contains(wanted))
                {
                    int width = Math.Max(25, proteins[i].organism.Length + 5);
                    writer.WriteLine(proteins[i].organism.PadRight(width) + proteins[i].name);
                    found = true;
                }
            }

            if (!found) writer.WriteLine("NOT FOUND");
        }

        static void Diff(List<Protein> proteins, string name1, string name2,
                         int operationNumber, StreamWriter writer)
        {
            writer.WriteLine(Number(operationNumber) + "   diff   " + name1 + "   " + name2);
            writer.WriteLine("amino-acids difference:");

            int index1 = FindProtein(proteins, name1);
            int index2 = FindProtein(proteins, name2);

            if (index1 == -1 || index2 == -1)
            {
                List<string> missing = new List<string>();
                if (index1 == -1) missing.Add(name1);
                if (index2 == -1) missing.Add(name2);
                writer.WriteLine("MISSING: " + string.Join(", ", missing));
                return;
            }

            string a = proteins[index1].amino_acids;
            string b = proteins[index2].amino_acids;

            int shortest = Math.Min(a.Length, b.Length);
            int difference = 0;
            for (int i = 0; i < shortest; i++)
            {
                if (a[i] != b[i]) difference++;
            }

            // лишний хвост более длинной цепочки тоже считается различием
            difference += Math.Abs(a.Length - b.Length);

            writer.WriteLine(difference);
        }

        static void Mode(List<Protein> proteins, string name,
                         int operationNumber, StreamWriter writer)
        {
            writer.WriteLine(Number(operationNumber) + "   mode   " + name);
            writer.WriteLine("amino-acid occurs:");

            int index = FindProtein(proteins, name);
            if (index == -1)
            {
                writer.WriteLine("MISSING: " + name);
                return;
            }

            // счётчики для букв A..Z: counts[0] - буква A, counts[1] - B, ...
            int[] counts = new int[26];
            foreach (char ch in proteins[index].amino_acids)
            {
                if (ch >= 'A' && ch <= 'Z') counts[ch - 'A']++;
            }

            // ищем максимум; знак > оставляет при ничьей букву, стоящую раньше в алфавите
            int bestIndex = 0;
            for (int i = 1; i < 26; i++)
            {
                if (counts[i] > counts[bestIndex]) bestIndex = i;
            }

            char bestLetter = (char)('A' + bestIndex);
            writer.WriteLine(bestLetter.ToString().PadRight(11) + counts[bestIndex]);
        }

        // Выбирает операцию по имени команды и вызывает нужный метод
        static void CommandHandler(List<Protein> proteins, List<Command> commands,
                                   StreamWriter writer)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                int operationNumber = i + 1;

                if (commands[i].name == "search")
                    Search(proteins, commands[i].parameter1, operationNumber, writer);
                else if (commands[i].name == "diff")
                    Diff(proteins, commands[i].parameter1, commands[i].parameter2,
                         operationNumber, writer);
                else if (commands[i].name == "mode")
                    Mode(proteins, commands[i].parameter1, operationNumber, writer);

                writer.WriteLine(Line);
            }
        }

        static void Main(string[] args)
        {
            string sequencesFile = "sequences.0.txt";
            string commandsFile = "commands.0.txt";
            string outputFile = "genedata.txt";

            // имена файлов можно передать аргументами командной строки
            if (args.Length >= 1) sequencesFile = args[0];
            if (args.Length >= 2) commandsFile = args[1];
            if (args.Length >= 3) outputFile = args[2];

            List<Protein> proteins = ReadData(sequencesFile);
            List<Command> commands = ReadCommands(commandsFile);

            using (StreamWriter writer = new StreamWriter(outputFile))
            {
                writer.WriteLine("Daniil Mackevich");
                writer.WriteLine("Genetic Searching");
                writer.WriteLine(Line);

                CommandHandler(proteins, commands, writer);
            }

            Console.WriteLine("Готово! Результат в файле " + outputFile);
        }
    }
}
