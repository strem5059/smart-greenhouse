using System;
using Godot;

namespace SmartGreenhouse.Agriculture;

public static class CropVisualBuilder
{
    private const float Tau = 6.283185307f;

    public static void Build(PlantController plant, string cropId)
    {
        switch (cropId)
        {
            case "carrot":
                BuildCarrot(plant);
                break;
            case "tomato":
                BuildTomato(plant);
                break;
            case "cucumber":
                BuildCucumber(plant);
                break;
            case "spinach":
                BuildSpinach(plant);
                break;
        }
    }

    private static void BuildCarrot(PlantController plant)
    {
        var foliage = Material(0.3f, 0.72f, 0.28f);
        var root = Material(0.95f, 0.55f, 0.15f);
        Cylinder(plant, "SeedlingVisual/Stem", 0.025f, 0.12f, foliage, new Vector3(0, 0.07f, 0));
        for (int i = 0; i < 3; i++)
        {
            Leaf(plant, $"SeedlingVisual/Frond{i}", foliage, 0.14f, 0.025f, i * Tau / 3, 0.5f, 0.10f);
        }

        Cylinder(plant, "GrowingVisual/Stem", 0.04f, 0.25f, foliage, new Vector3(0, 0.13f, 0));
        for (int i = 0; i < 5; i++)
        {
            Leaf(plant, $"GrowingVisual/Frond{i}", foliage, 0.24f, 0.04f, i * Tau / 5, 0.45f, 0.14f);
        }

        Node3D mature = Stage(plant, "MatureVisual");
        foreach (Node child in mature.GetChildren())
        {
            if (child is Node3D n)
            {
                n.Visible = false;
            }
        }

        Cylinder(plant, "MatureVisual/Stem", 0.035f, 0.32f, foliage, new Vector3(0, 0.17f, 0));
        for (int i = 0; i < 6; i++)
        {
            Leaf(plant, $"MatureVisual/Frond{i}", foliage, 0.32f, 0.05f, i * Tau / 6, 0.5f, 0.2f);
        }

        Cone(plant, "MatureVisual/CarrotRoot", 0.09f, 0.22f, root, new Vector3(0, 0.03f, 0));
    }

    private static void BuildTomato(PlantController plant)
    {
        var leaf = Material(0.18f, 0.5f, 0.2f);
        var stem = Material(0.15f, 0.4f, 0.12f);
        var fruit = Material(0.9f, 0.2f, 0.15f);
        Cylinder(plant, "SeedlingVisual/Stem", 0.03f, 0.14f, stem, new Vector3(0, 0.07f, 0));
        Cylinder(plant, "GrowingVisual/Stem", 0.04f, 0.36f, stem, new Vector3(0, 0.18f, 0));
        for (int i = 0; i < 3; i++)
        {
            Leaf(plant, $"GrowingVisual/Branch{i}", leaf, 0.18f, 0.06f, i * Tau / 3, 0.3f, 0.18f + i * 0.05f);
        }

        Cylinder(plant, "MatureVisual/MainStem", 0.045f, 0.6f, stem, new Vector3(0, 0.3f, 0));
        for (int i = 0; i < 5; i++)
        {
            Leaf(plant, $"MatureVisual/Branch{i}", leaf, 0.22f, 0.07f, i * Tau / 5, 0.35f, 0.2f + i * 0.06f);
        }

        for (int i = 0; i < 3; i++)
        {
            float angle = i * Tau / 3;
            Sphere(plant, $"MatureVisual/Tomato{i}", 0.07f, fruit, new Vector3(Mathf.Cos(angle) * 0.1f, 0.32f + i * 0.07f, Mathf.Sin(angle) * 0.1f));
        }
    }

    private static void BuildCucumber(PlantController plant)
    {
        var vine = Material(0.25f, 0.6f, 0.2f);
        var leaf = Material(0.35f, 0.75f, 0.3f);
        var fruit = Material(0.2f, 0.5f, 0.18f);
        for (int i = 0; i < 3; i++)
        {
            Leaf(plant, $"SeedlingVisual/Leaf{i}", leaf, 0.1f, 0.06f, i * Tau / 3, 0.3f, 0.05f);
        }

        Cylinder(plant, "GrowingVisual/Vine", 0.02f, 0.45f, vine, new Vector3(0.02f, 0.05f, 0), new Vector3(0, 0, Mathf.DegToRad(65)));
        for (int i = 0; i < 4; i++)
        {
            Leaf(plant, $"GrowingVisual/Leaf{i}", leaf, 0.14f, 0.09f, i * Tau / 4, 0.5f, 0.06f);
        }

        Cylinder(plant, "MatureVisual/Vine", 0.025f, 0.6f, vine, new Vector3(0.05f, 0.05f, 0), new Vector3(0, 0, Mathf.DegToRad(70)));
        for (int i = 0; i < 5; i++)
        {
            Leaf(plant, $"MatureVisual/Leaf{i}", leaf, 0.18f, 0.12f, i * Tau / 5, 0.5f, 0.06f);
        }

        Cylinder(plant, "MatureVisual/Cucumber1", 0.04f, 0.16f, fruit, new Vector3(0.15f, 0.06f, 0.02f), new Vector3(Mathf.DegToRad(20), 0, 0));
        Cylinder(plant, "MatureVisual/Cucumber2", 0.045f, 0.18f, fruit, new Vector3(-0.12f, 0.06f, 0.05f), new Vector3(Mathf.DegToRad(-15), 0, 0));
    }

    private static void BuildSpinach(PlantController plant)
    {
        var dark = Material(0.16f, 0.48f, 0.22f);
        var mid = Material(0.24f, 0.62f, 0.3f);
        for (int i = 0; i < 2; i++)
        {
            Leaf(plant, $"SeedlingVisual/Lance{i}", mid, 0.13f, 0.045f, i * Mathf.Pi, 0.35f, 0.05f);
        }

        for (int i = 0; i < 4; i++)
        {
            Leaf(plant, $"GrowingVisual/Lance{i}", dark, 0.2f, 0.07f, i * Tau / 4, 0.45f, 0.07f);
        }

        Node3D mature = Stage(plant, "MatureVisual");
        foreach (Node child in mature.GetChildren())
        {
            if (child is Node3D n)
            {
                n.Visible = false;
            }
        }

        for (int i = 0; i < 6; i++)
        {
            Leaf(plant, $"MatureVisual/Lance{i}", dark, 0.28f, 0.09f, i * Tau / 6, 0.5f, 0.09f);
        }
    }

    private static Node3D Stage(PlantController plant, string path) => plant.GetNode<Node3D>(path);

    private static StandardMaterial3D Material(float r, float g, float b) => new() { AlbedoColor = new Color(r, g, b), Roughness = 0.6f };

    private static void Leaf(PlantController plant, string path, StandardMaterial3D material, float length, float width, float angle, float tilt, float height)
    {
        var mesh = new SphereMesh { Radius = width, Height = length };
        var instance = new MeshInstance3D { Mesh = mesh, MaterialOverride = material };
        var pivot = new Node3D { Position = new Vector3(0, height, 0), Rotation = new Vector3(0, -angle, 0) };
        instance.Rotation = new Vector3(0, 0, -tilt);
        instance.Position = new Vector3(length * 0.4f, 0, 0);
        AddTo(plant, path, pivot);
        pivot.AddChild(instance);
    }

    private static void Sphere(PlantController plant, string path, float radius, StandardMaterial3D material, Vector3 position)
    {
        var mesh = new SphereMesh { Radius = radius, Height = radius * 2 };
        var instance = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position };
        AddTo(plant, path, instance);
    }

    private static void Cylinder(PlantController plant, string path, float radius, float height, StandardMaterial3D material, Vector3 position, Vector3? rotation = null)
    {
        var mesh = new CylinderMesh { TopRadius = radius * 0.8f, BottomRadius = radius, Height = height };
        var instance = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position };
        if (rotation != null)
        {
            instance.Rotation = rotation.Value;
        }

        AddTo(plant, path, instance);
    }

    private static void Cone(PlantController plant, string path, float radius, float height, StandardMaterial3D material, Vector3 position)
    {
        var mesh = new CylinderMesh { TopRadius = 0.01f, BottomRadius = radius, Height = height };
        var instance = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position };
        AddTo(plant, path, instance);
    }

    private static void AddTo(PlantController plant, string path, Node node)
    {
        int slash = path.LastIndexOf('/');
        Node parent = slash > 0 ? plant.GetNode(path[..slash]) : plant;
        node.Name = path[(slash + 1)..];
        parent.AddChild(node);
    }
}
