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
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Compatibility.GameData.Universe
{
	public class TexturePanels : IniFile
	{
		public List<string> Files { get; private set; }
		public List<string> TextureShapes { get; private set; }
		public Dictionary<string,TextureShape> Shapes { get; private set; }

		public TexturePanels(string filename)
		{
			var parsed = ParseFile (filename);

            Shapes = new Dictionary<string, TextureShape>(StringComparer.InvariantCultureIgnoreCase);
			Files = new List<string>();
			TextureShapes = new List<string>();
			foreach (var s in parsed)
			{
				if (s.Name.ToUpperInvariant() != "TEXTURE")
					throw new Exception("Invalid section " + s.Name + " in " + filename);
				Add(s);
			}
		}

		void Add(Section section)
		{
			
			string current_texname = null;
			string f = null;
			for (int i = 0; i < section.Count; i++)
			{
				Entry e = section [i];
				switch (e.Name.ToLowerInvariant())
				{
				case "file":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					if (f != null) throw new Exception("Duplicate " + e.Name + " Entry in " + section.Name);
					Files.Add(e[0].ToString());
					f = e[0].ToString();
					break;
				case "texture_name":
					if (e.Count != 1) throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					current_texname = e [0].ToString ();
					break;
				case "tex_shape":
						Shapes.Add(e[0].ToString(),
									new TextureShape(
									e[0].ToString(),
									e[0].ToString(),
									new RectangleF(0, 0, 1, 1)
								));
					break;
				case "shape_name":
					if (e.Count != 1)
						throw new Exception ("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
					var shape_name = e [0].ToString ();
						RectangleF dimensions;
						if (i + 1 >= section.Count || section[i + 1].Name.ToLower() != "dim")
						{
							dimensions = new RectangleF(0, 0, 1, 1);
						}
						else
						{
							e = section[i + 1];
							if (e.Name != "dim")
								throw new Exception("expected dim, got " + e.Name);
							if (e.Count != 4)
								throw new Exception("Invalid number of values in " + section.Name + " Entry " + e.Name + ": " + e.Count);
							dimensions = new RectangleF(e[0].ToSingle(), e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
						}
					
					Shapes.Add(shape_name,
						new TextureShape(
						current_texname,
						shape_name,
						dimensions
					));
					i++;
					break;
				default: throw new Exception("Invalid Entry in " + section.Name + ": " + e.Name);
				}
			}
		}

		public override string ToString()
		{
			return "TextureShapes";
		}
	}
}