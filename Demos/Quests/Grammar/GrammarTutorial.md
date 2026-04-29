# Grammar Terminal Field Reference

This document explains every supported `<field type="..."/>` value used inside terminal definitions in your grammar XML.

## Table of Contents
1. Overview
2. Field Attributes
3. Primitive Types
4. Special Types
5. List Types
6. Full Terminal Example
7. Adding New Field Types
8. Best Practices

---

## 1. Overview

A terminal represents a final definition in a grammar file. for example:
```
  <rule id="Goto">
    <one-of>
      <item> explore </item>
      <item> <ruleref uri="#Learn" /> go to </item>
      <item> go to </item>
      <item> take </item>
    </one-of>
  </rule>
 ```
 in here the rule <i>Goto</i> can be interpreted as <i>explore</i>, <i>go to</i>, <i>take</i> or a combination of terminals
 that represent <i>Learn</i> followed by a <i>go to</i>. 
 
 Terminals/Nodes are then added to an <b>LBS Quest Layer</b>, where a sequence of nodes will represent a quest. 
 
 These nodes would be more practical if they could store data from the level or a designer's input, in order
 to achieve this, GrammarFields are used. They are defined as `<field type="..."/>` inside a terminal definition.


```xml
<terminal id="take">
    <field type="int" name="Amount" />
</terminal>
```

Then when loading a grammar into the LBS tool, when a node is placed their terminal definition's fields, 
can be accesssed and edited in the inspector, allowing designers to input values or reference level data.

---

## 2. Field Attributes

| Attribute | Purpose |
|---|---|
| `type=""` | Determines the data type |
| `name=""` | Label shown in editor |

Example:

```xml
<field type="string" name="NPC Name" />
```

---

## 3. Primitive Types

### 3.1 `int`
Stores whole numbers.

```xml
<field type="int" name="Amount" />
```

### 3.2 `float`
Stores decimal numbers.

```xml
<field type="float" name="Speed" />
```

### 3.3 `string`
Stores text values.

```xml
<field type="string" name="NPC Name" />
```

### 3.4 `Bool`
Stores a boolean.
```xml
<field type="bool" name="Loop patrol" />
```


---

## 4. Special Types

### 4.1 `referenceType`
Stores a Bundle/Object type reference.

```xml
<field type="referenceType" name="Berry Type" />
```

### 4.2 `referenceGraph`
Stores a graph target in the level. Such as a tile, including its layer, position and Bundle.

```xml
<field type="referenceGraph" name="Boss Key" />
```

### 4.3 `Color`
Stores a color.
```xml
<field type="color" name="Area Tint" />
```

### 4.4 `eventHook`
Stores n methods from a GameObject(monobehavior) in the level.

```xml
<field type="eventHook" name="On Complete" />
```

### 4.5 `area`
Stores a rectangle `(x, y, width, height)`.

```xml
<field type="area" name="Spawn Area" />
```

---

## 5. List Types

Lists allow multiple values of the same type. 
as long as the type is supported, you can create a list of it by prefixing it with `List.`

Syntax:

```xml
List.<type>
```

Example:

```xml
<field type="List.referenceType" name="Rewards" />
```

---

## 6. Full Terminal Example

```xml
<terminal id="take">
    <field type="referenceType" name="Item Type" />
    <field type="int" name="Item Amount" />
    <field type="List.int" name="Item Prices" />
    <field type="string" name="Item Name" />
</terminal>
```

---

## 7. Adding New Field Types

1. Create a new `GrammarField<T>` class.
2. Create its Visual Element inspector. UXML and C#.
3. Register it in `NodeDataBehaviourEditor`, dictionary `fieldTypeToFieldEditor`.
4. Register it in `CreateField(...)` method in `GrammarField` abstract class.

Example:

```csharp
public class GrammarVector3 : GrammarField<Vector3> { }
```

---


