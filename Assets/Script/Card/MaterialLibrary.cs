using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public class MaterialLibrary : MonoBehaviour
{

    // a library that record the relation between all material. All material in the librrary have at least one Decompose result, and there should also be a dictionary about how two material can compose a new material
    // the new mateiral is record in the list as well

    public static List<Material> MaterialLists;
    public static List<Material> Materialunlocked;




    public List<Material> Decompose(Material material, int type)
    {
        if (type ==1)
            return material.Composition;
        else
            return material.Composition2;
    }

    public void Compose_2(Material material1, Material material2)
    {
        // new Material Result = 

        //return Result;

    }

    public void Compose_3(Material material1, Material material2, Material material3)
    {

        // new Material Result = 

        //return Result;
    }
}
