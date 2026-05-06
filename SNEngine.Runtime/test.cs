using SNEngine.API;

public class testScene : SNScript
{
    public testScene()
    {
        SceneName = "testScene";
    }

    public override void Execute()
    {
        SNEngine.API.SNEngine.LoadEmptyScene();
        BackgroundAPI.Show("beach");
    }

    private void hello()
    {
        CharacterAPI.Show("Nagatoro", "happy");
    }
}