using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CreatureParticleEnumGenerator
{
    private const string SpriteFolder = "Assets/Sprites/CreatureParticles/";
    private const string OutputFile = "Assets/Scripts/Creatures/CreatureParticle.cs";

    [MenuItem("Tools/Regenerate Creature Particle Enum")]
    public static void Generate()
    {
        var guids = AssetDatabase.FindAssets("t:texture2D", new[] { SpriteFolder });

        var names = guids
            .Select(g => AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(g), typeof(Texture2D)).name)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var sb = new StringBuilder();

        sb.AppendLine("public enum CreatureParticle");
        sb.AppendLine("{");

        foreach (var name in names)
            sb.AppendLine($"    {name},");

        sb.AppendLine("}");

        File.WriteAllText(OutputFile, sb.ToString());

        AssetDatabase.Refresh();

        Debug.Log($"Generated CreatureParticle enum with {names.Count} entries.");
    }
}