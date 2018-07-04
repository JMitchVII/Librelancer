﻿/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer;
using LibreLancer.Utf.Cmp;

namespace LancerEdit
{
    //Class for keeping hardpoints node references
    public class ModelNodes
    {
        public List<ModelNode> Nodes = new List<ModelNode>();
    }
    public class ModelNode
    {
        public string Name;
        public LUtfNode Node;
        public LUtfNode HardpointsNode;

        public void HardpointsToNodes(List<HardpointDefinition> hps)
        {
            if(hps.Count == 0) {
                if(HardpointsNode != null) {
                    Node.Children.Remove(HardpointsNode);
                    HardpointsNode = null;
                }
                return;
            }
            if(HardpointsNode == null) {
                HardpointsNode = new LUtfNode() { Name = "Hardpoints", Parent = Node };
                Node.Children.Add(HardpointsNode);
            }
            HardpointsNode.Children = new List<LUtfNode>();
            LUtfNode fix = null;
            LUtfNode rev = null;
            if(hps.Any((x) => x is FixedHardpointDefinition)) {
                fix = new LUtfNode() { Name = "Fixed", Parent = Node };
                fix.Children = new List<LUtfNode>();
                HardpointsNode.Children.Add(fix);
            }
            if(hps.Any((x) => x is RevoluteHardpointDefinition)) {
                rev = new LUtfNode() { Name = "Revolute", Parent = Node };
                rev.Children = new List<LUtfNode>();
                HardpointsNode.Children.Add(rev);
            }
            foreach(var hp in hps) {
                var node = new LUtfNode() { Name = hp.Name, Children = new List<LUtfNode>() };
                node.Children.Add(new LUtfNode()
                {
                    Name = "Orientation", Parent = node,
                    Data = UnsafeHelpers.CastArray(new float[] {
                        hp.Orientation.M11, hp.Orientation.M12, hp.Orientation.M13,
                        hp.Orientation.M21, hp.Orientation.M22, hp.Orientation.M23,
                        hp.Orientation.M31, hp.Orientation.M32, hp.Orientation.M33
                    })
                });
                node.Children.Add(new LUtfNode()
                {
                    Name = "Position", Parent = node,
                    Data = UnsafeHelpers.CastArray(new float[] { hp.Position.X, hp.Position.Y, hp.Position.Z })
                });
                if(hp is FixedHardpointDefinition) {
                    node.Parent = fix;
                    fix.Children.Add(node);
                }   else {
                    var revolute = (RevoluteHardpointDefinition)hp;
                    node.Children.Add(new LUtfNode()
                    {
                        Name = "Axis",Parent = node,
                        Data = UnsafeHelpers.CastArray(new float[] {
                            revolute.Axis.X, revolute.Axis.Y, revolute.Axis.Z
                        })
                    });
                    node.Children.Add(new LUtfNode()
                    {
                        Name = "Min", Parent = node,
                        Data = BitConverter.GetBytes(revolute.Min)
                    });
                    node.Children.Add(new LUtfNode()
                    {
                        Name = "Max", Parent = node,
                        Data = BitConverter.GetBytes(revolute.Max)
                    });
                    node.Parent = rev;
                    rev.Children.Add(node);
                }
            }
        }
    }
}
