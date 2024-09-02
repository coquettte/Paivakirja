using System.Security.Cryptography;
using System.Text;
using System.IO;

class Program
{
    static List<DiaryEntry> diaryEntries = new List<DiaryEntry>();
    static string filePath = "diaryEntries.txt";
    private static string encryptionKey = "myencryptionkey123";

    static void Main(string[] args)
    {
        LoadEntriesFromFile();

        bool running = true;

        while (running)
        {
            Console.WriteLine("1. Lisää merkintä");
            Console.WriteLine("2. Tarkastele merkintöjä");
            Console.WriteLine("3. Muokkaa merkintää");
            Console.WriteLine("4. Poista merkintä");
            Console.WriteLine("5. Hae merkintöjä");
            Console.WriteLine("6. Poistu\n");

            string choice = Console.ReadLine() ?? string.Empty;

            switch (choice)
            {
                case "1":
                    AddEntry();
                    break;
                case "2":
                    ViewEntries();
                    break;
                case "3":
                    EditEntry();
                    break;
                case "4":
                    DeleteEntry();
                    break;
                case "5":
                    SearchEntries();
                    break;
                case "6":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Virheellinen valinta, yritä uudelleen.");
                    break;
            }
        }

        SaveEntriesToFile();
    }

    static void AddEntry()
    {
        Console.WriteLine("Syötä otsikko:");
        string? title = Console.ReadLine();

        Console.WriteLine("Syötä sisältö:");
        string? content = Console.ReadLine();

        DiaryEntry entry = new DiaryEntry
        {
            Date = DateTime.Now,
            Title = title ?? string.Empty,
            Content = content ?? string.Empty
        };

        diaryEntries.Add(entry);
        Console.WriteLine("Merkintä lisätty!");
    }


    static void ViewEntries()
    {
        Console.WriteLine("\nPäiväkirjamerkinnät:");

        if (diaryEntries.Count == 0)
        {
            Console.WriteLine("Ei merkintöjä.");
        }
        else
        {
            for (int i = 0; i < diaryEntries.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {diaryEntries[i].Date} - {diaryEntries[i].Title} - {diaryEntries[i].Content}");
            }
        }

        Console.WriteLine();
    }

    static void EditEntry()
    {
        ViewEntries();

        if (diaryEntries.Count == 0)
            return;

        Console.WriteLine("Syötä muokattavan merkinnän numero:");
        if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= diaryEntries.Count)
        {
            DiaryEntry entry = diaryEntries[index - 1];

            Console.WriteLine($"Muokataan merkintää: {entry.Title ?? "N/A"}");

            Console.WriteLine("Syötä uusi otsikko (jätä tyhjäksi, jos ei muutosta):");
            string? newTitle = Console.ReadLine();
            if (!string.IsNullOrEmpty(newTitle))
            {
                entry.Title = newTitle;
            }

            Console.WriteLine("Syötä uusi sisältö (jätä tyhjäksi, jos ei muutosta):");
            string? newContent = Console.ReadLine();
            if (!string.IsNullOrEmpty(newContent))
            {
                entry.Content = newContent;
            }

            Console.WriteLine("Merkintä päivitetty!");
        }
        else
        {
            Console.WriteLine("Virheellinen numero.");
        }
    }


    static void DeleteEntry()
    {
        ViewEntries();

        if (diaryEntries.Count == 0)
            return;

        Console.WriteLine("Syötä poistettavan merkinnän numero:");
        if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= diaryEntries.Count)
        {
            diaryEntries.RemoveAt(index - 1);
            Console.WriteLine("Merkintä poistettu!");
        }
        else
        {
            Console.WriteLine("Virheellinen numero.");
        }
    }

    static void SearchEntries()
    {
        Console.WriteLine("Syötä hakusana (otsikko tai sisältö):");
        string searchTerm = Console.ReadLine() ?? string.Empty;

        var matchingEntries = diaryEntries.FindAll(e =>
            e.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            e.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        if (matchingEntries.Count > 0)
        {
            Console.WriteLine("\nLöydetyt merkinnät:");
            foreach (var entry in matchingEntries)
            {
                Console.WriteLine($"{entry.Date} - {entry.Title} - {entry.Content}\n");
            }
        }
        else
        {
            Console.WriteLine("Ei merkintöjä, jotka vastaavat hakusanaa.");
        }
    }

    static void SaveEntriesToFile()
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var entry in diaryEntries)
            {
                string line = $"{entry.Date}|{entry.Title}|{entry.Content}";
                string encryptedLine = Encrypt(line, encryptionKey);
                writer.WriteLine(encryptedLine);
            }
        }
    }

    static void LoadEntriesFromFile()
    {
        if (File.Exists(filePath))
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    string decryptedLine = Decrypt(line, encryptionKey);
                    var parts = decryptedLine.Split('|');
                    if (parts.Length == 3)
                    {
                        DiaryEntry entry = new DiaryEntry
                        {
                            Date = DateTime.Parse(parts[0]),
                            Title = parts[1],
                            Content = parts[2]
                        };
                        diaryEntries.Add(entry);
                    }
                }
            }
        }
    }

    static string Encrypt(string plainText, string key)
    {
        using (Aes aesAlg = Aes.Create())
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                aesAlg.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
            aesAlg.IV = new byte[16]; // Alustusvektori (IV), oletusarvoisesti nollattu

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    static string Decrypt(string cipherText, string key)
    {
        using (Aes aesAlg = Aes.Create())
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                aesAlg.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
            aesAlg.IV = new byte[16]; // Alustusvektori (IV), oletusarvoisesti nollattu

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }

}

class DiaryEntry
{
    public DateTime Date { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
}
