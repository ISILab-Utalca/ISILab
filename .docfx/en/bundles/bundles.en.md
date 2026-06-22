---
title: 'LBS - Bundles'
draft: false
weight: 16
tags: [ "Documentation", "LBS", "Tutorial" ]
author: ["Ignacio Reyes"]
---

# LBS Bundles

## What are Bundles?

“Bundles” are intermediary scriptable objects that wrap different game assets, models , prefabs and its metadata used by LBS to generate interior, exterior, and population layer content. A bundle can contain one or more prefabs inside. A bundle can also contain characteristics, characteristics are the base metadata that LBS uses to know how to place bundles (and their assets) in the 3D generation process.

The dimensions and settings of each prefab will depend exclusively on the users at the moment of using the 3D generation tab we recommend check the transform of each prefab and secure its in {position: }

---

## Bundle Collections

"Bundle Collections" are scriptable objects that contain a list of bundles. It is recommended to use Bundle Collections for same-layer-bundles. You may also separate same-layer-bundles in different Bundle Collections depending on their purpose. Example: A project with 4 Bundle Collections, in which two of them are population layers, one for props and one for lighting assets; InteriorLayer_MB, ExteriorLayer_MB, PopulationAssets_MB and PopulationLighting_MB.

---

## Types of Bundles

Here are the types of bundles some of the considerations to keep in mind when creating Bundle's Prefabs depending on their destined layer.

### Interior Layer Bundles

The elements of this layer consist mainly of floors, doors, walls and corners.

Prefab’s considerations:
 - It is recommended for their position to be zero in the plains X, Y, and Z. If not, they will be generated in the scene in their corresponding position.
 - The dimensions or sizes, of all elements must be the same. If a same group of tiles has different dimensions, there will be overlap and/or empty spaces between tiles when generating the level in the scene.
 - The directions and pivots will depend on the type of tile, for example:

### Exterior Layer Bundles

This layer works mainly with connecting ground tiles. To be able to asign the bundle's connections it is necessary to know the rotation of the object respect of its Y axis. In the shown example, according to the image, the positive side of the Z axis has a grass section, and positive X has a section of grass with path

Prefab’s considerations:
- The visible part of the plain must be in the positive Y direction.
- Position is recommended to be zero in the X, Y, and Z axis. Otherwise, it will be generated in the scene in its corresponding position.
- Dimensions or sizes of all tiles must be the same, if not, when generating the level there will appear problems with overslap or empty spaces
- Pivot must be at the center of the model. (In it's X and Z axis)

### Population Layer Bundles

Population is basically everything related to extra elements or props in the scenario; Decoration, enemies, npc’s, items, etc.

Prefab’s considerations:
- Position is recommended to be zero in the plains X, Y, and Z. If not, it will be generated in the scene in its corresponding position.
- The dimensions or sizes of all elements must be the same. If a same group of tiles has different dimensions, there will be overlap and/or empty spaces between tiles when generating the level in the scene.
- The direction of all elements must be pointing at the same plain, either at the X or Z axis.
- Pivots are recommended to be all at the inferior center (0, 0, 0), as shown in the image.

> [!NOTE]
> You can have more than one layer of the same type in a level, this may be useful in order to keep some assets in its own layer.  
> Example: Having multiple population layers in order to separate foliage from interactive objects.

---

## Bundle Configuration

### LBS's Tags

Before configuring bundles and their characteristics, the user must create a couple “LBS Tags” depending on their project and assets, this section describes LBS Tags in detail.

LBS Tags are Scriptable Objects containing information about interior, exterior and population bundles. These are the necessary tags that should be in the project before the Bundle’s Characteristics setup. Some tags are intended to be used only in Main Bundles

- **LBS Tags for Interior Layer**: These Tags come included with LBS, and they are used to categorize interior assets; Wall, Floor, Corner, Door, Window.
Interior layer tags are used by the 3D generator and the interior layer assistant.
- **LBS Tags for Exterior Layer**: Here we can find tags for each different type of terrain, for example; Grass, Water, Snow, Sand, etc. Exterior layer tags are used by the 3D generator and the WFC tool.
- **LBS Tags for Population Layer**:  Population bundles need tags referencing their use case or type of population, for example; decoration, item, enemy, lighting, etc. Population layer tags are used by the Map Elites assistant.

### Bundle's Characteristics

In order to generate bundles as users intend, LBS needs extra information about them, we refer to this data as “characteristics”. The characteristics of each bundle will depend mainly on their corresponding layer.

Characteristics can be individually added in the inspector, or in batch with the help of the Bundle Manager Window. 

Characteristic’s setup must be manually made for each Bundle, if two or more bundles share the same characteristic’s setup, you can copy and paste each characteristic through Unity’s inspector.

#### List of Bundle’s Characteristics

Here is a list of all Bundle’s characteristics with their corresponding layer, some characteristics are designed to be used only in Main Bundles

##### Characteristics for Main Bundles
- **LBS Tags Characteristic**: This characteristic holds a LBS tag, use it to add a tag to any bundle
- **LBS Main Interior Bundle**: Use this characteristic so LBS can recognize your bundle as a Main Interior Bundle
- **LBS Main Exterior Bundle**: Use this characteristic so LBS can recognize your bundle as a Main Exterior Bundle
- **LBS Main Population Bundle**: Use this characteristic so LBS can recognize your bundle as a Main Population Bundle

##### Characteristics for Exterior layer Bundles
- **LBS Direction**: Indicates the type of terrain associated to each direction of the asset by adding a LBSTag in each edge (Edge-based exterior layer) or corner (Vertex-based exterior layer).
- **LBS Terrain Connection Grid** [Only for Main Exterior Bundle]: It is used to indicate valid grid patterns
- **LBS Directioned Group** [Only for Main Exterior Bundle]: Indicates the spawning probability of each sub bundle with the “LBS Direction” characteristic, it can then be stored as a WFC preset
- **WFC Presets Characteristic** [Only for Main Exterior Bundle]: Storages different WFCs Presets.
- **LBS Navigable Tags** [Only for Main Exterior Bundle]: Indicates which “LBSTags” corresponds to navigable terrain, it is used by the Map Elites Assistant.
