using SNEngine.API;
using SNEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class testIf : SNScript
{
    public testIf()
    {
        SceneName = "testIf";
    }

    public override void Execute()
    {
        SNEngine.API.SNEngine.LoadEmptyScene();
        SetVar("playerHealth", 35);
        SetVar("enemyLevel", 20);
        if (GetVar("playerHealth").AsInt() < 50)
        {
            Debug.Log("You are injured!");
            SetVar("enemyLevel", 30);
        }
        else
        {
            Debug.Log("You are strong!");
        }

        Debug.Log("Current enemy level: " + GetVar("enemyLevel"));
    }
}