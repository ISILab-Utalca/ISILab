using ISILab.LBS.Characteristics;
using ISILab.LBS.CustomComponents;
using ISILab.LBS.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using static ISILab.LBS.Modules.ConnectedTileMapModule;
using static ISILab.LBS.Characteristics.LBSDirectionedChance;
using UnityEngine;

namespace ISILab.LBS.VisualElements
{
    /// <summary>
    /// Shows what was captured using CaptureChance() in AssistantWFC. It orders the information into a treeview, in which
    /// the data can be partially manipulated, like the chance, but not which new tiles will appear.
    /// </summary>
    [LBSCustomEditor("Connections group chance", typeof(LBSDirectionedChance))]
    public class LBSDirectionedChanceEditor : LBSCustomEditor
    {
        public VisualElement content;

        public LBSDirectionedChanceEditor()
        {
        }

        public LBSDirectionedChanceEditor(object target) : base(target)
        {
            CreateVisualElement();
            SetInfo(target);
        }


        public override void SetInfo(object paramTarget)
        {
            this.target = paramTarget;
            var target = paramTarget as LBSDirectionedChance;

            if (target == null)
                return;

            //target._Update();

            content = new VisualElement();
            Add(content);

            var tiletype = new DropdownField
            {
                label = "Tile Type",
                choices = System.Enum.GetNames(typeof(ConnectedTileType)).ToList(),
                value = target.currentType.ToString()
            };

            tiletype.RegisterValueChangedCallback(evt =>
            {
                if (System.Enum.TryParse<ConnectedTileType>(evt.newValue, out var result))
                {
                    target.currentType = result;
                }
            });

            content.Add(tiletype);

            content.Add(new VisualElement() { style = { height = 10 } });

            List<TileDirection> weights = target.tileDirections;

            // Show warning if there are no child bundles to add weights
            if (weights.Count <= 0)
            {
                var wp = new WarningPanel("This feature adds chances to the child bundles, " +
                                          "make sure to have child bundles for this feature to work.");

                content.Add(wp);
                return;
            }

            //An LBSCustomTreeView is used to show the information.
            //The counter is for the ID that TreeNodeData asks for.

            int counter = target.tileDirections.Count;
            var list = new LBSCustomTreeView();
            var finaldata = new List<TreeViewItemData<TreeNodeData>>();

            Dictionary<int, TileDirectionChance> chancePool = new();

            //For each tile direction captured..
            for (int i = 0; i < target.tileDirections.Count; i++)
            {
                //Register the main bundles
                TileDirection tileDir = target.tileDirections[i];
                var parentNode = new TreeNodeData
                {
                    Id = ++counter,
                    Label = $"{tileDir.mainTarget.BundleName} (Rotation = {tileDir.rotation})" ,
                    Type = NodeType.Label
                };

                //And then register it's sub-bundles with their respective direction and chance.
                var directionNodes = new List<TreeViewItemData<TreeNodeData>>();

                if (tileDir.chances.Any())
                {
                    for (int d = 0; d < tileDir.chances.Count; d++)
                    {
                        //Depending on which direction it will appear, from 0 (right) to 3 (down), clockwise
                        int dirValue = d;
                        var dirNode = new TreeNodeData
                        {
                            Id = ++counter,
                            Label = LBSDirection.Directions[dirValue],
                            Type = NodeType.Label
                        };

                        
                        var chanceNodes = new List<TreeViewItemData<TreeNodeData>>();

                        //For each rotation that the tile might've gotten:
                        //for (int k = 0; k < 4; k++)
                        {
                            foreach (TileDirectionChance chance in tileDir.chances[d].list)
                            {
                                chanceNodes.Add(
                                    new TreeViewItemData<TreeNodeData>(++counter, new TreeNodeData
                                    {
                                        Id = counter,
                                        Label = chance.target.name + $" (Rotation = {chance.rotation})",
                                        Type = NodeType.Label
                                    })
                                );
                                chanceNodes.Add(
                                    new TreeViewItemData<TreeNodeData>(++counter, new TreeNodeData
                                    {
                                        Id = counter,
                                        Label = "Chance",
                                        SliderValue = chance.chance,
                                        Type = NodeType.Slider
                                    })
                                );
                                chancePool.Add(counter, chance);
                            }
                        }
                        

                        directionNodes.Add(new TreeViewItemData<TreeNodeData>(++counter, dirNode, chanceNodes));
                    }
                }


                finaldata.Add(new TreeViewItemData<TreeNodeData>(i, parentNode, directionNodes));
            }



            /*
            var treeData = new List<TreeViewItemData<TreeNodeData>>
            {
                new TreeViewItemData<TreeNodeData>(0, new TreeNodeData { Id = 0, Label = "Grupo 1", Type = NodeType.Label }, new List<TreeViewItemData<TreeNodeData>>
                {
                    new TreeViewItemData<TreeNodeData>(1, new TreeNodeData { Id = 1, Label = "Texto", Type = NodeType.Label }),
                    new TreeViewItemData<TreeNodeData>(2, new TreeNodeData { Id = 2, Label = "Peso", SliderValue = 0.5f, Type = NodeType.Slider }),
                    new TreeViewItemData<TreeNodeData>(3, new TreeNodeData { Id = 3, Label = "Objeto", ObjectFieldValue = null, Type = NodeType.ObjectField })
                })
            };
            */

            HashSet<(TreeNodeData, Slider)> weightNodes = new();

            var maxLimit = new Slider("Max weight", 0f, 1f) { value = target.maxLimit, showInputField = true };
            maxLimit.RegisterValueChangedCallback(evt =>
            {
                target.maxLimit = evt.newValue;
                bool first = true;
                foreach((TreeNodeData, Slider) node in weightNodes)
                {
                    if (first)
                    {
                        first = false;
                        //Debug.Log($"Slider value = {node.Item1.SliderValue}. evt.newValue = {evt.newValue}.");
                    }
                    float realValue = Mathf.Min(node.Item1.SliderValue ?? evt.newValue, evt.newValue);
                    chancePool[node.Item1.Id].chance = realValue;
                    node.Item2.SetValueWithoutNotify(realValue);
                }
            });

            content.Add(maxLimit);

            content.Add(new VisualElement() { style = { height = 20 } });

            list.SetRootItems(finaldata);
            list.makeItem = () => {
                var ve = new VisualElement();

                ve.ClearClassList();
                ve.AddToClassList("lbs-tree-view-item");

                return ve;
            };

            list.bindItem = (element, index) =>
            {
                element.Clear();
                var nodeData = list.GetItemDataForIndex<TreeNodeData>(index);

                var label = new Label(nodeData.Label);
                label.AddToClassList("lbs-tree-view-item");

                switch (nodeData.Type)
                {
                    case NodeType.Label:
                        element.Add(label);
                        break;
                    case NodeType.Slider:
                        float sliderValue = nodeData.SliderValue ?? 0f;
                        var slider = new Slider(label.text, 0, 1) { value = Mathf.Min(sliderValue, maxLimit.value), showInputField = true };
                        slider.RegisterValueChangedCallback(evt =>
                        {
                            nodeData.SliderValue = evt.newValue;
                            if(evt.newValue > maxLimit.value)
                            {
                                chancePool[nodeData.Id].chance = maxLimit.value;
                                nodeData.SliderValue = maxLimit.value;
                                slider.SetValueWithoutNotify(maxLimit.value);
                            }
                            else 
                            {
                                chancePool[nodeData.Id].chance = evt.newValue;
                                nodeData.SliderValue = evt.newValue;
                            }
                        });
                        slider.AddToClassList("lbs-tree-view-item");
                        slider.labelElement.style.minWidth = 40;
                        slider.style.marginRight = 10;
                        element.Add(slider);
                        weightNodes.Add((nodeData, slider));
                        break;
                    case NodeType.ObjectField:
                        var objField = new LBSCustomObjectField { objectType = typeof(Object), value = nodeData.ObjectFieldValue };
                        objField.RegisterValueChangedCallback(evt => nodeData.ObjectFieldValue = evt.newValue);
                        objField.AddToClassList("lbs-tree-view-item");
                        element.Add(label);
                        element.Add(objField);
                        break;
                }

                element.AddToClassList("lbs-tree-view-item");
            };

            list.unbindItem = (element, index) =>
            {
                var slider = element.Q<Slider>();
                if (slider == null) return;
                var nodeData = list.GetItemDataForIndex<TreeNodeData>(index);
                if(nodeData == null) return;
                weightNodes.Remove((nodeData, slider));
            };

            content.Add(list);



            /*
            // Instance the weights of the child bundles
            for (int i = 0; i < weights.Count; i++)
            {
                var current = weights[i];
                var box = new VisualElement();
                content.Add(box);

                box.Add(new Label(current.mainTarget.Name));

                var slider = new Slider();
                slider.showInputField = true;
                box.Add(slider);
                slider.lowValue = 0;
                slider.highValue = 1;
                slider.value = current.weight;
                slider.RegisterValueChangedCallback(evt =>
                {
                    current.weight = evt.newValue;
                });
            }
            */


        }

        protected override VisualElement CreateVisualElement()
        {
            var target = this.target as LBSDirectionedChance;

            content = new VisualElement();
            content.AddToClassList("lbs-tree-view");

            return this;
        }
    }

    public class TreeNodeData
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public float? SliderValue { get; set; }
        public UnityEngine.Object ObjectFieldValue { get; set; }
        public NodeType Type { get; set; }
        public List<TreeNodeData> Children { get; set; } = new();

    }

    public enum NodeType
    {
        Label,
        Slider,
        ObjectField
    }
}

