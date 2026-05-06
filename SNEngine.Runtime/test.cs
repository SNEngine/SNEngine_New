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
        SetVar("myVar2", 15);
        SetVar("MyVar25", GetVar("3.68"));
        SetVar("myvar80", GetVar("myVar25"));
    }
}