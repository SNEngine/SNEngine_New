using SNEngine.API;
using SNEngine.Audio;
using System;
using System.Linq;

namespace SNEngine.Test;

class Program
{
    private static bool _isPreviewMode = false;

    static void Main(string[] args)
    {
        ParseCommandLineArguments(args);

        Console.WriteLine("=== SNEngine.API Test Started ===");
        if (_isPreviewMode)
            Console.WriteLine("[Preview Mode] Shared Memory enabled for Studio");
        Console.WriteLine("Press ESC in the game window to close.\n");

        string title = _isPreviewMode 
            ? "SNEngine Test (Preview)" 
            : "SNEngine Test Window";

        SNEngine.API.SNEngine.OnInitialized += async () =>
        {
            SNEngine.Core.Debug.Log("Engine initialized! Loading visual novel scene...");
            // LoadDefaultPackages() is called automatically early (AssetManagerInitialized).
            // The method is idempotent, so an extra call here is safe but unnecessary.
            SNEngine.API.SNEngine.LoadEmptyScene();
            BackgroundAPI.Show("classroom_day");
            CharacterAPI.AddExampleYuki();

            // Fully automatic positioning to the bottom of the screen.
            // Bounce (feet line) is calculated from the actual pixel data of the sprite.
            // Character will sit correctly without legs being cut off.
            CharacterAPI.Show("yuki", "happy");
            SNEngine.API.SNEngine.LoadScreen("dialog");
            SNEngine.API.SNEngine.LoadScreen("dialog-onscreen");
            SNEngine.API.SNEngine.LoadScreen("fps");
          //  SNEngine.API.SNEngine.LoadScreen("test_images", zIndex: 18);


            SNEngine.Core.Debug.Log("Scene loaded via SNEngine.API");
            await Task.Delay(1000);
            await OnScreenDialogueAPI.Think("Sed ut perspiciatis, unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam eaque ipsa, quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt, explicabo. Nemo enim ipsam voluptatem, quia voluptas sit, aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos, qui ratione voluptatem sequi nesciunt, neque porro quisquam est, qui dolorem ipsum, quia dolor sit, amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt, ut labore et dolore magnam aliquam quaerat voluptatem. Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem vel eum iure reprehenderit, qui in ea voluptate velit esse, quam nihil molestiae consequatur, vel illum, qui dolorem eum fugiat, quo voluptas nulla pariatur? At vero eos et accusamus et iusto odio dignissimos ducimus, qui blanditiis praesentium voluptatum deleniti atque corrupti, quos dolores et quas molestias excepturi sint, obcaecati cupiditate non provident, similique sunt in culpa, qui officia deserunt mollitia animi, id est laborum et dolorum fuga. Et harum quidem rerum facilis est et expedita distinctio. Nam libero tempore, cum soluta nobis est eligendi optio, cumque nihil impedit, quo minus id, quod maxime placeat, facere possimus, omnis voluptas assumenda est, omnis dolor repellendus. Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe eveniet, ut et voluptates repudiandae sint et molestiae non recusandae. Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus maiores alias consequatur aut perferendis doloribus asperiores repellat.");
            MusicAPI.SetPlaylist(new[]
{
    "audio/music/music1.mp3",
    "audio/music/music2.mp3",
    "audio/music/music3.mp3"
});

            MusicAPI.Shuffle = true;
            MusicAPI.Repeat = MusicRepeatMode.All;
            MusicAPI.CrossfadeSeconds = 5f;

            MusicAPI.Play();     // или просто MusicAPI.Play();

            await Task.Delay(1000);
            CharacterAPI.Hide("yuki");
            await CharacterAPI.Say("yuki", GenerateRandomText(450));
        };

        // Используем удобный публичный API
        SNEngine.API.SNEngine.Run(
            windowTitle: title,
            width: 1280,
            height: 720,
            useSharedMemoryPreview: _isPreviewMode
        );
    }

    private static string GenerateRandomText(int targetLength)
    {
        const string loremBase = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,!? ";
        var random = new Random();
        var result = new System.Text.StringBuilder();

        // Генерируем слова разной длины
        while (result.Length < targetLength)
        {
            int wordLength = random.Next(3, 12); // слова от 3 до 12 символов

            for (int i = 0; i < wordLength; i++)
            {
                result.Append(loremBase[random.Next(loremBase.Length)]);
            }

            result.Append(' '); // пробел между словами
        }

        // Обрезаем до нужной длины и делаем первую букву заглавной
        string text = result.ToString().TrimEnd();
        if (text.Length > targetLength)
        {
            text = text.Substring(0, targetLength);
        }

        // Делаем первую букву большой
        if (text.Length > 0)
        {
            text = char.ToUpper(text[0]) + text.Substring(1);
        }

        return text + ".";
    }

    private static void ParseCommandLineArguments(string[] args)
    {
        _isPreviewMode = args.Any(a =>
            a.Equals("--preview", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-preview", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("/preview", StringComparison.OrdinalIgnoreCase));
    }
}