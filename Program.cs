using System.IO.Compression;

namespace sp_model_edit
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SP Model Edit is a tool to edit an Argos Translate model file.");
            string inputModel = args[0];
            string outputModel = args[1];

            // Unzip model file
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            ZipFile.ExtractToDirectory(inputModel, tempDir);
            // Move files from each folder to root
            foreach (string dir in Directory.GetDirectories(tempDir))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    File.Move(file, Path.Combine(tempDir, Path.GetFileName(file)));
                }
                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    Directory.Move(subDir, Path.Combine(tempDir, Path.GetFileName(subDir)));
                }
                Directory.Delete(dir);
            }
            Console.WriteLine("sp_model_edit: ");
            string command = Console.ReadLine();

            if (command == "change")
            {

                byte[] sentencePieceModel = System.IO.File.ReadAllBytes(tempDir + "\\sentencepiece.model");

                Console.WriteLine("Find: ");
                string find = Console.ReadLine();
                Console.WriteLine("Change: ");
                string change = Console.ReadLine();

                // Loop until find is found in model
                for (int i = 0; i < sentencePieceModel.Length; i++)
                {
                    if (sentencePieceModel[i] == find[0])
                    {
                        bool found = true;
                        for (int j = 0; j < find.Length; j++)
                        {
                            if (sentencePieceModel[i + j] != find[j])
                            {
                                found = false;
                                break;
                            }
                        }
                        if (found)
                        {
                            if (sentencePieceModel[i - 4] == find.Length + 3)
                            {
                                Console.WriteLine("Found at: " + i);
                                // Add to array if length is different
                                if (change.Length > find.Length)
                                {
                                    List<byte> l = sentencePieceModel.ToList();
                                    l.InsertRange(i + find.Length, new byte[change.Length - find.Length]);
                                    sentencePieceModel = l.ToArray();
                                }
                                // Change
                                for (int j = 0; j < change.Length; j++)
                                {
                                    sentencePieceModel[i + j] = (byte)change[j];
                                }
                                // Remove from array if length is different
                                if (change.Length < find.Length)
                                {
                                    List<byte> l = sentencePieceModel.ToList();
                                    l.RemoveRange(i + change.Length, find.Length - change.Length);
                                    sentencePieceModel = l.ToArray();
                                }
                                sentencePieceModel[i - 4] = (byte)(change.Length + 3);
                                sentencePieceModel[i - 6] = (byte)(change.Length + 10);
                                break;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
                // Read model\shared_vocabulary.txt
                string sharedVocabulary = File.ReadAllText(tempDir + "\\model\\shared_vocabulary.txt");
                // Change first occurence of find (so we can't use .Replace)
                int index = sharedVocabulary.IndexOf(find);
                if (index > 0)
                {
                    sharedVocabulary = sharedVocabulary.Substring(0, index) + change + sharedVocabulary.Substring(index + find.Length);
                }
                // Write model\shared_vocabulary.txt
                File.WriteAllText(tempDir + "\\model\\shared_vocabulary.txt", sharedVocabulary);
                // Write output
                File.WriteAllBytes(tempDir + "\\sentencepiece.model", sentencePieceModel);
                ZipFile.CreateFromDirectory(tempDir, outputModel, CompressionLevel.Optimal, true);
                Directory.Delete(tempDir, true);
            }
        }
    }
}