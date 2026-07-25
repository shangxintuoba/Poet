using System;
using System.Collections.Generic;
using UnityEngine;

public class MaterialLibrary : MonoBehaviour
{
    [Serializable]
    public class Recipe
    {
        public List<Material> Ingredients = new List<Material>();
        public Material Result;
    }

    public List<Material> MaterialLists = new List<Material>();
    public List<Material> Materialunlocked = new List<Material>();
    [SerializeField] private List<Recipe> recipes = new List<Recipe>();

    public List<Material> Decompose(Material material, int type)
    {
        if (material == null)
            return new List<Material>();

        List<Material> composition = type == 1
            ? material.Composition
            : material.Composition2;

        return composition != null
            ? new List<Material>(composition)
            : new List<Material>();
    }

    public Material Compose_2(Material material1, Material material2)
    {
        return Compose(material1, material2);
    }

    public Material Compose_3(Material material1, Material material2, Material material3)
    {
        return Compose(material1, material2, material3);
    }

    private Material Compose(params Material[] materials)
    {
        foreach (Material material in materials)
        {
            if (material == null || material is Raw)
                return null;
        }

        foreach (Recipe recipe in recipes)
        {
            if (recipe.Result != null && HasSameIngredients(recipe.Ingredients, materials))
            {
                if (!Materialunlocked.Contains(recipe.Result))
                    Materialunlocked.Add(recipe.Result);

                return recipe.Result;
            }
        }

        return null;
    }

    private static bool HasSameIngredients(List<Material> recipeIngredients, Material[] materials)
    {
        if (recipeIngredients == null || recipeIngredients.Count != materials.Length)
            return false;

        var remaining = new List<Material>(recipeIngredients);
        foreach (Material material in materials)
        {
            if (!remaining.Remove(material))
                return false;
        }

        return remaining.Count == 0;
    }
}