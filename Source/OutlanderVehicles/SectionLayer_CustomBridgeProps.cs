using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace OutlanderVehicles;

[StaticConstructorOnStartup]
public abstract class SectionLayer_CustomBridgeProps : SectionLayer
{

    static SectionLayer_CustomBridgeProps()
	{
		LongEventHandler.ExecuteWhenFinished(delegate
		{
			foreach (TerrainDef allDef in DefDatabase<TerrainDef>.AllDefsListForReading)
			{
				if (((Def)allDef).HasModExtension<TerrainExtension_CustomBridgeProps>())
				{
					TerrainExtension_CustomBridgeProps modExtension = ((Def)allDef).GetModExtension<TerrainExtension_CustomBridgeProps>();
					modExtension.rightMat = MaterialPool.MatFrom(modExtension.rightPath, ShaderDatabase.Transparent, allDef.color);
					modExtension.loopMat = MaterialPool.MatFrom(modExtension.loopPath, ShaderDatabase.Transparent, allDef.color);
				}
			}
		});
	}

    public SectionLayer_CustomBridgeProps(Section section)
        : base(section)
    {
        base.relevantChangeTypes = MapMeshFlagDefOf.Terrain;
    }

    public override bool Visible => DebugViewSettings.drawTerrain;

    public override void Regenerate()
    {
        ClearSubMeshes(MeshParts.All);
        var terrainGrid = Map.terrainGrid;
        var cellRect = section.CellRect;
        var num = AltitudeLayer.TerrainScatter.AltitudeFor();
        foreach (var intVec in cellRect)
        {
            if (!ShouldDrawPropsBelow(intVec, terrainGrid))
            {
                continue;
            }

            var c = intVec;
            TerrainDef c1 = terrainGrid.TerrainAt(c);
            c.x++;
            Material material;
            if (c.InBounds(Map) && ShouldDrawPropsBelow(c, terrainGrid) && c1 == terrainGrid.TerrainAt(c))
            {
                material = ((Def)c1).GetModExtension<TerrainExtension_CustomBridgeProps>().loopMat;
            }
            else
            {
                material = ((Def)c1).GetModExtension<TerrainExtension_CustomBridgeProps>().rightMat;
            }

            var subMesh = GetSubMesh(material);
            var count = subMesh.verts.Count;
            subMesh.verts.Add(new Vector3(intVec.x, num, intVec.z - 0.9f));
            subMesh.verts.Add(new Vector3(intVec.x, num, intVec.z));
            subMesh.verts.Add(new Vector3(intVec.x + 1, num, intVec.z));
            subMesh.verts.Add(new Vector3(intVec.x + 1, num, intVec.z - 0.9f));
            subMesh.uvs.Add(new Vector2(0f, 0.1f));
            subMesh.uvs.Add(new Vector2(0f, 1f));
            subMesh.uvs.Add(new Vector2(1f, 1f));
            subMesh.uvs.Add(new Vector2(1f, 0.1f));
            subMesh.tris.Add(count);
            subMesh.tris.Add(count + 1);
            subMesh.tris.Add(count + 2);
            subMesh.tris.Add(count);
            subMesh.tris.Add(count + 2);
            subMesh.tris.Add(count + 3);
        }

        FinalizeMesh(MeshParts.All);
    }

    private bool ShouldDrawPropsBelow(IntVec3 c, TerrainGrid terrGrid)
    {
        var terrainDef = terrGrid.TerrainAt(c);
        bool result;
        if (terrainDef == null || !terrainDef.layerable || !((Def)terrainDef).HasModExtension<TerrainExtension_CustomBridgeProps>())
        {
            result = false;
        }
        else
        {
            var c2 = c;
            c2.z--;
            TerrainDef c3 = terrGrid.TerrainAt(c2);
            if (!c2.InBounds(Map))
            {
                result = false;
            }
            else
            {
                var terrain = terrGrid.TerrainAt(c2);
                result = (int)((BuildableDef)c3).passability == 2 || c2.SupportsStructureType(Map,((BuildableDef)terrainDef).terrainAffordanceNeeded)  && !c3.layerable &&
                                                                    !((Def)c3).HasModExtension<TerrainExtension_CustomBridgeProps>() && !c3.bridge;
                //(!IsTerrainThisBridge(terrain) && c2.SupportsStructureType(Map, TerrainBridgelikeDefOf.Bridgelike));
                //result = ( c2.SupportsStructureType(Map, sectionTile), ((BuildableDef)terrainDef).terrainAffordanceNeeded)) && !c3.layerable && !((Def)val3).HasModExtension<TerrainExtension_CustomBridgeProps>() && !c3.bridge
            }
        }

        return result;
    }
}