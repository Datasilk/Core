//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using Newtonsoft.Json;

//public struct structScaffold
//{
//    public Dictionary<string, string> Data;
//    public Dictionary<string, string> arguments;
//    public List<structScaffoldElement> elements;
//}

//public struct structScaffoldElement
//{
//    public string name;
//    public string htm;
//}

//public class ScaffoldChild
//{
//    private Scaffold _parent;
//    private string _id;
//    public ScaffoldDictionary Data;

//    public ScaffoldChild(Scaffold parent, string id)
//    {
//        _parent = parent;
//        _id = id;
//        Data = new ScaffoldDictionary(parent, id);
//    }

    
//}

//public class ScaffoldDictionary : Dictionary<string, string>
//{
//    private Scaffold _parent;
//    private string _id;
//    public ScaffoldDictionary(Scaffold parent, string id)
//    {
//        _parent = parent;
//        _id = id;
//    }

//    public string this[string key]
//    {
//        get
//        {
//            return _parent.Data[_id + "-" + key];
//        }
//        set
//        {
//            _parent.Data[_id + "-" + key] = value;
//        }
//    }
//}

//public class Scaffold
//{
//    [JsonIgnore]
//    private Core S;

//    public Dictionary<string, string> Data;
//    public List<structScaffoldElement> elements;
//    public string serializedElements;
//    public string HTML = "";
//    public string sectionName = "";
//    public ScaffoldChild _child = null;

//    public ScaffoldChild Parent(string id)
//    {
//        return new ScaffoldChild(this, id);
//    }
        
//    public Scaffold(Core DatasilkCore, string file = "", string html = "", string section = "")
//    {
//        S = DatasilkCore;
//        Data = new Dictionary<string, string>();
//        sectionName = section;

//        if (S.Server.Scaffold.ContainsKey(file + '/' + section) == false)
//        {
//            elements = new List<structScaffoldElement>();

//            //first, check if html is already provided
//            HTML = html;
//            if(HTML == "")
//            {
//                //try loading file from disk or cache next
//                if (S.Server.Cache.ContainsKey(file) == false)
//                {
//                    HTML = File.ReadAllText(S.Server.MapPath(file));
//                }
//                else
//                {
//                    HTML = (string)S.Server.Cache[file];
//                }
//            }
//            if(HTML == "") { return; }

//            //next, find the group of code matching the scaffold section name
//            if (section != "")
//            {
//                //find starting tag (optionally with arguments)
//                //for example: {{button (name:submit, style:outline)}}
//                int[] e = new int[3];
//                e[0] = HTML.IndexOf("{{" + section);
//                if (e[0] >= 0)
//                {
//                    e[1] = HTML.IndexOf("}", e[0]);
//                    if (e[1] - e[0] <= 256)
//                    {
//                        e[1] = HTML.IndexOf("{{/" + section + "}}", e[1]);
//                    }
//                    else { e[0] = -1; }

//                }

//                if (e[0] >= 0 & e[1] > (e[0] + section.Length + 4))
//                {
//                    e[2] = e[0] + 4 + section.Length;
//                    HTML = HTML.Substring(e[2], e[1] - e[2]);
//                }
//            }

//            //get scaffold from html code
//            var dirty = true;
//            while (dirty == true) {
//                dirty = false;
//                var arr = HTML.Split("{{");
//                var i = 0;
//                var u = 0;
//                var u2 = 0;
//                structScaffoldElement scaff;

//                //types of scaffold elements

//                // {{title}}                        = variable
//                // {{address}} {{/address}}         = block
//                // {{button "/ui/button-medium"}}   = HTML include

//                //first, load all HTML includes
//                for (var x = 0; x < arr.Length; x++)
//                {
//                    if(x == 0 && HTML.IndexOf(arr[x]) == 0)
//                    {
//                        arr[x] = "{!}" + arr[x];
//                    }
//                    else if (arr[x].Trim() != "")
//                    {
//                        i = arr[x].IndexOf("}}");
//                        u = arr[x].IndexOf('"');
//                        if (i > 0 && u > 0 && u < i - 2)
//                        {
//                            scaff.name = arr[x].Substring(0, u - 1).Trim();
//                            u2 = arr[x].IndexOf('"', u + 2);
//                            //load the scaffold HTML
//                            var newScaff = new Scaffold(S, arr[x].Substring(u + 1, u2 - u - 1));

//                            //rename child scaffold variables with a prefix of "scaff.name-"
//                            var ht = newScaff.HTML;
//                            var y = 0;
//                            var prefix = scaff.name + "-";
//                            while(y >= 0)
//                            {
//                                y = ht.IndexOf("{{", y);
//                                if(y < 0) { break; }
//                                if(ht.Substring(y+2,1) == "/")
//                                {
//                                    ht = ht.Substring(0, y + 3) + prefix + ht.Substring(y + 3);
//                                }
//                                else
//                                {
//                                    ht = ht.Substring(0, y + 2) + prefix + ht.Substring(y + 2);
//                                }
//                                y += 2;
//                            }
//                            arr[x] = "{!}" + ht + arr[x].Substring(i + 2);
//                            HTML = JoinHTML(arr);
//                            dirty = true; //HTML is dirty, restart loop
//                            break;
//                        }
//                    }
                    
//                }
//                if(dirty == false)
//                {
//                    //next, process variables & blocks
//                    for (var x = 0; x < arr.Length; x++)
//                    {
//                        if (x == 0 && HTML.IndexOf(arr[x].Substring(3)) == 0)
//                        {
//                            elements.Add(new structScaffoldElement() { htm = arr[x].Substring(3), name = "" });
//                        }
//                        else if (arr[x].Trim() != "")
//                        {
//                            i = arr[x].IndexOf("}}");
//                            u = arr[x].IndexOf('"');
//                            scaff = new structScaffoldElement();
//                            if (i > 0)
//                            {
//                                scaff.htm = arr[x].Substring(i + 2);
//                                scaff.name = arr[x].Substring(0, i).Trim();
//                            }
//                            else
//                            {
//                                scaff.name = "";
//                                scaff.htm = arr[x];
//                            }
//                            elements.Add(scaff);
//                        }
//                    }
//                }
//            }
//            //caching
//            if (S.Server.environment != Server.enumEnvironment.development)
//            {
//                //cache the scaffold file
//                var scaffold = new structScaffold();
//                scaffold.Data = Data;
//                scaffold.elements = elements;
//                S.Server.Scaffold.Add(file + '/' + section, scaffold);
//            }
//        }
//        else
//        {
//            //get scaffold object from memory
//            var scaffold = S.Server.Scaffold[file + '/' + section];
//            Data = scaffold.Data;
//            elements = scaffold.elements;
//        }
//        serializedElements = S.Util.Serializer.WriteObjectToString(elements);
//    }

//    private string JoinHTML(string[] html)
//    {
//        for(var x = 0; x < html.Length; x++)
//        {
//            switch (html[x].Substring(0, 3))
//            {
//                case "{!}":
//                    html[x] = html[x].Substring(3);
//                    break;
//                default:
//                    html[x] = "{{" + html[x];
//                    break;
//            }
//        }


//        return string.Join("", html);
//    }

//    public string Render()
//    {
//        return Render(Data);
//    }

//    public string Render(Dictionary<string, string> nData)
//    {
//        //deserialize list of elements since we will be manipulating the list,
//        //so we don't want to permanently mutate the public elements array
//        var elems = (List<structScaffoldElement>)S.Util.Serializer.ReadObject(serializedElements, typeof(List<structScaffoldElement>));
//        if (elems.Count > 0)
//        {
//            //render scaffold with paired nData data
//            var scaff = new StringBuilder();
//            var s = "";
//            var useScaffold = false;
//            var closing = new List<List<string>>();

//            //remove any unwanted blocks of HTML from scaffold
//            for (var x = 0; x < elems.Count; x++)
//            {
//                if (x < elems.Count - 1)
//                {
//                    for (var y = x + 1; y < elems.Count; y++)
//                    {
//                        //check for closing tag
//                        if (elems[y].name == "/" + elems[x].name)
//                        {
//                            //add enclosed group of HTML to list for removing
//                            List<string> closed = new List<string>();
//                            closed.Add(elems[x].name);
//                            closed.Add(x.ToString());
//                            closed.Add(y.ToString());

//                            if (nData.ContainsKey(elems[x].name) == true)
//                            {
//                                //check if user wants to include HTML 
//                                //that is between start & closing tag   
//                                s = nData[elems[x].name];
//                                if (string.IsNullOrEmpty(s) == true) { s = ""; }
//                                if (s == "true" | s == "1")
//                                {
//                                    closed.Add("true");
//                                }
//                                else { closed.Add(""); }
//                            }
//                            else { closed.Add(""); }

//                            closing.Add(closed);
//                        }
//                    }

//                }
//            }

//            //remove all groups of HTML in list that should not be displayed
//            List<int> removeIndexes = new List<int>();
//            bool isInList = false;
//            for (int x = 0; x < closing.Count; x++)
//            {
//                if (closing[x][3] != "true")
//                {
//                    //add range of indexes from closing to the removeIndexes list
//                    for (int y = int.Parse(closing[x][1]); y < int.Parse(closing[x][2]); y++)
//                    {
//                        isInList = false;
//                        for (int z = 0; z < removeIndexes.Count; z++)
//                        {
//                            if (removeIndexes[z] == y) { isInList = true; break; }
//                        }
//                        if (isInList == false) { removeIndexes.Add(y); }
//                    }
//                }
//            }

//            //physically remove HTML list items from scaffold
//            int offset = 0;
//            for (int z = 0; z < removeIndexes.Count; z++)
//            {
//                elems.RemoveAt(removeIndexes[z] - offset);
//                offset += 1;
//            }

//            //finally, replace scaffold variables with custom data
//            for (var x = 0; x < elems.Count; x++)
//            {
//                //check if scaffold item is an enclosing tag or just a variable
//                useScaffold = true;
//                if (elems[x].name.IndexOf('/') < 0)
//                {
//                    for (int y = 0; y < closing.Count; y++)
//                    {
//                        if (elems[x].name == closing[y][0]) { useScaffold = false; break; }
//                    }
//                }
//                else { useScaffold = false; }

//                if ((nData.ContainsKey(elems[x].name) == true
//                || elems[x].name.IndexOf('/') == 0) & useScaffold == true)
//                {
//                    //inject string into scaffold variable
//                    s = nData[elems[x].name.Replace("/", "")];
//                    if (string.IsNullOrEmpty(s) == true) { s = ""; }
//                    scaff.Append(s + elems[x].htm);
//                }
//                else
//                {
//                    //passively add htm, ignoring scaffold variable
//                    scaff.Append(elems[x].htm);
//                }
//            }

//            //render scaffolding as HTML string
//            return scaff.ToString();
//        }
//        return "";
//    }

//    public string Get(string name)
//    {
//        var index = elements.FindIndex(c => c.name == name);
//        if(index < 0) { return "";  }
//        var part = elements[index];
//        var html = part.htm;
//        for (var x = index + 1; x < elements.Count; x++)
//        {
//            part = elements[x];
//            if (part.name == "/" + name) { break; }

//            //add inner scaffold elements
//            if (part.name.IndexOf('/') < 0)
//            {
//                if (Data.ContainsKey(part.name))
//                {
//                    if (Data[part.name] == "1" || Data[part.name].ToLower() == "true")
//                    {
//                        html += Get(part.name);
//                    }
//                }
//            }
//            else
//            {
//                html += part.htm;
//            }

//        }

//        return html;
//    }
//}
