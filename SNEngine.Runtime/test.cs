using SNEngine.API;
using SNEngine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UltimateStressTest : SNScript
{
    public UltimateStressTest()
    {
        SceneName = "UltimateStressTest";
    }

    public override void Execute()
    {
        SNEngine.API.SNEngine.LoadEmptyScene();
        SetVar("playerHealth", 85);
        SetVar("playerMana", 42.7);
        SetVar("playerName", "Nagatoro");
        SetVar("isBossFight", true);
        SetVar("hasPotion", false);
        SetVar("score", 1250);
        SetVar("enemyCount", 7);
        SetVar("damageMultiplier", 1.5);
        SetVar("criticalChance", 0.75);
        Debug.Log("=== Ultimate Stress Test Started ===");
        if (GetVar("playerHealth").AsInt() > 80)
        {
            Debug.Log("You are in perfect condition!");
        }
        else
        {
            Debug.Log("You are critically injured!");
            SetVar("playerHealth", 30);
        }

        if (GetVar("playerLevel").AsInt() >= 10 && GetVar("enemyHealth").AsInt() < 50 || GetVar("score").AsInt() > 2000)
        {
            Debug.Log("You have a huge advantage!");
            SetVar("score", GetVar("score").AsInt() + 750);
        }
        else if (GetVar("playerLevel").AsInt() < 5 || (GetVar("enemyHealth").AsInt() > 80 && GetVar("hasPotion").AsBool() == false))
        {
            Debug.Log("This is extremely dangerous...");
            SetVar("playerHealth", GetVar("playerHealth").AsInt() - 25);
        }

        if (GetVar("isBossFight").AsBool() == true)
        {
            Debug.Log("BOSS FIGHT!");
            Debug.Log("You used healing potion!");
            SetVar("playerHealth", 100);
            SetVar("hasPotion", false);
        }
        else
        {
            Debug.Log("No potions left... desperate mode!");
            SetVar("playerHealth", GetVar("playerHealth").AsInt() + 15);
        }

        Debug.Log("Normal fight.");
        if (GetVar("score").AsInt() >= 1000 && GetVar("playerHealth").AsInt() > 60)
        {
            Debug.Log("Excellent performance!");
            SetVar("finalScore", GetVar("score").AsInt() * GetVar("damageMultiplier").AsInt());
            Debug.Log("Final score: " + GetVar("finalScore").AsInt());
        }
        else
        {
            Debug.Log("You survived... barely.");
        }

        if (GetVar("playerName").AsString() == "Nagatoro")
        {
            Debug.Log("Nagatoro is fighting with you!");
        }
        else
        {
            Debug.Log("Unknown character detected.");
        }

        if (GetVar("damageMultiplier").AsInt() > 1.0 && GetVar("criticalChance").AsInt() > 0.7)
        {
            Debug.Log("Critical hit possible!");
            SetVar("enemyHealth", GetVar("enemyHealth").AsInt() - 35);
        }

        Debug.Log("=== Test finished ===");
        Debug.Log("Final Health: " + GetVar("playerHealth"));
        Debug.Log("Final Score: " + GetVar("score"));
        Debug.Log("Potions left: " + GetVar("hasPotion"));
    }
}