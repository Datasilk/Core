using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public struct SerializedView
{
    public ViewData Data;
    public Dictionary<string, int[]> Fields;
    public List<ViewElement> Elements;
    public List<ViewPartial> Partials;
}

[Serializable]
public struct ViewElement
{
    public string Name;
    public string Htm;
    public Dictionary<string, string> Vars;
}

public static class ViewCache
{
    public static Dictionary<string, SerializedView> cache { get; set; } = new Dictionary<string, SerializedView>();

    public static void Remove(string file, string section = "")
    {
        if (cache.ContainsKey(file + '/' + section) == true)
        {
            cache.Remove(file + '/' + section);
        }
    }

    public static void Clear()
    {
        cache.Clear();
    }
}

public class ViewChild
{
    public ViewDictionary Data { get; set; }
    public Dictionary<string, int[]> Fields = new Dictionary<string, int[]>();

    public ViewChild(View parent, string id)
    {
        Data = new ViewDictionary(parent, id);
        //load related fields
        foreach (var item in parent.Fields)
        {
            if (item.Key.IndexOf(id + "-") == 0)
            {
                Fields.Add(item.Key.Replace(id + "-", ""), item.Value);
            }
        }
    }

    public string this[string key]
    {
        get
        {
            return Data[key];
        }
        set
        {
            Data[key] = value;
        }
    }

    public void Show(string blockKey)
    {
        Data[blockKey] = "True";
    }

    /// <summary>
    /// Binds an object to the view template. Use e.g. {{myprop}} or {{myobj.myprop}} to represent object fields & properties in template
    /// </summary>
    /// <param name="obj"></param>
    public void Bind(object obj, string root = "")
    {
        if (obj != null)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj))
            {
                object val = property.GetValue(obj);
                var name = (root != "" ? root + "." : "") + property.Name.ToLower();
                if (val == null)
                {
                    Data[name] = "";
                }
                else if (val is string || val is int || val is long || val is double || val is decimal || val is short)
                {
                    //add property value to dictionary
                    Data[name] = val.ToString();
                }
                else if (val is bool)
                {
                    Data[name] = (bool)val == true ? "1" : "0";
                }
                else if (val is DateTime)
                {
                    Data[name] = ((DateTime)val).ToShortDateString() + " " + ((DateTime)val).ToShortTimeString();
                }
                else if (val is object)
                {
                    //recurse child object for properties
                    Bind(val, name);
                }
            }
        }
    }
}

public class ViewDictionary : Dictionary<string, string>
{
    private View _parent;
    private string _id;
    public ViewDictionary(View parent, string id)
    {
        _parent = parent;
        _id = id;
    }

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
    public string this[string key]
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
    {
        get
        {
            return _parent[_id + "-" + key];
        }
        set
        {
            _parent[_id + "-" + key] = value;
        }
    }
}

[Serializable]
public class ViewPartial
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Prefix { get; set; } //prefix used in html variable names after importing the partial
}

/// <summary>
/// Allow developers to create pointers so that the mustache variable can use a pointer path instead of the actual relative path to a partial view file
/// </summary>
public static class ViewPartialPointers
{
    public static List<KeyValuePair<string, string>> Paths { get; set; } = new List<KeyValuePair<string, string>>();
}

public class ViewData : IDictionary<string, string>
{
    private Dictionary<string, string> _data = new Dictionary<string, string>();

    public string this[string key]
    {
        get
        {
            return _data[key];
        }
        set
        {
            _data[key] = value;
        }
    }

    public bool this[string key, bool isBool]
    {
        get
        {
            if (_data[key] == "True")
            {
                return true;
            }
            return false;
        }

        set
        {
            if (value)
            {
                _data[key] = "True";
            }
            else
            {
                _data[key] = "False";
            }
        }
    }

    public ICollection<string> Keys => _data.Keys;

    public ICollection<string> Values => _data.Values;

    public int Count => _data.Count;

    public bool IsReadOnly => false;

    public void Add(string key, string value)
    {
        _data.Add(key, value);
    }

    public void Add(string key, bool value)
    {
        _data.Add(key, value.ToString());
    }

    public void Add(KeyValuePair<string, string> item)
    {
        _data.Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _data.Clear();
    }

    public bool Contains(KeyValuePair<string, string> item)
    {
        return _data.Contains(item);
    }

    public bool ContainsKey(string key)
    {
        return _data.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return _data.GetEnumerator();
    }

    public bool Remove(string key)
    {
        return _data.Remove(key);
    }

    public bool Remove(KeyValuePair<string, string> item)
    {
        if (_data.Contains(item))
        {
            return _data.Remove(item.Key);
        }
        return false;
    }

    public bool TryGetValue(string key, out string value)
    {
        return _data.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _data.GetEnumerator();
    }
}

public class ViewOptions
{
    public string Html { get; set; }
    public string File { get; set; }
    public string Section { get; set; }
}

public static class ViewConfig
{
    public static string Path { get; set; }
}

public class View
{
    public List<ViewElement> Elements;
    public List<ViewPartial> Partials = new List<ViewPartial>();
    public Dictionary<string, int[]> Fields = new Dictionary<string, int[]>();
    public string Filename = "";
    public string HTML = "";
    public string Section = ""; //section of the template to use for rendering

    private ViewData data;
    private Dictionary<string, ViewChild> children = null;

    public ViewChild Child(string id)
    {
        if (children == null)
        {
            children = new Dictionary<string, ViewChild>();
        }
        if (!children.ContainsKey(id))
        {
            children.Add(id, new ViewChild(this, id));
        }
        return children[id];
    }

    public View(ViewOptions options)
    {
        if (options.Html != null && options.Html != "")
        {
            Parse("", "", options.Html);
        }
        else
        {
            Parse(options.File, options.Section, "");
        }
    }

    /// <summary>
    /// Use a template file to bind data and replace mustache variables with data, e.g. {{my-name}} is replaced with value of View["my-name"]
    /// </summary>
    /// <param name="file">relative path to the template file</param>
    /// <param name="cache">Dictionary object used to save cached, parsed template to</param>
    public View(string file, Dictionary<string, SerializedView> cache = null)
    {
        Parse(file, "", "", cache);
    }

    /// <summary>
    /// Use a template file to bind data and replace mustache variables with data, e.g. {{my-name}} is replaced with value of View["my-name"]
    /// </summary>
    /// <param name="file">relative path to the template file</param>
    /// <param name="section">section name within the template file to load, e.g. {{my-section}} ... {{/my-section}}</param>
    /// <param name="cache">Dictionary object used to save cached, parsed template to</param>
    public View(string file, string section, Dictionary<string, SerializedView> cache = null)
    {
        Parse(file, section.ToLower(), "", cache);
    }

    public string this[string key]
    {
        get
        {
            if (!data.ContainsKey(key.ToLower()))
            {
                data.Add(key.ToLower(), "");
            }
            return data[key.ToLower()];
        }
        set
        {
            data[key.ToLower()] = value;
        }
    }

    public bool ContainsKey(string key)
    {
        return data.ContainsKey(key.ToLower());
    }

    public void Show(string blockKey)
    {
        data[blockKey.ToLower(), true] = true;
    }

    /// <summary>
    /// Clears any bound data, which is useful when reusing the same View object in a loop
    /// </summary>
    public void Clear()
    {
        data = new ViewData();
    }

    /// <summary>
    /// Binds an object to the view template. Use e.g. {{myprop}} or {{myobj.myprop}} to represent object fields & properties in template
    /// </summary>
    /// <param name="obj"></param>
    public void Bind(object obj, string root = "")
    {
        if (obj != null)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj))
            {
                object val = property.GetValue(obj);
                var name = (root != "" ? root + "." : "") + property.Name.ToLower();
                if (val == null)
                {
                    data[name] = "";
                }
                else if (val is string || val is int || val is long || val is double || val is decimal || val is short)
                {
                    //add property value to dictionary
                    data[name] = val.ToString();
                }
                else if (val is bool)
                {
                    data[name] = (bool)val == true ? "1" : "0";
                }
                else if (val is DateTime)
                {
                    data[name] = ((DateTime)val).ToShortDateString() + " " + ((DateTime)val).ToShortTimeString();
                }
                else if (val is object)
                {
                    //recurse child object for properties
                    Bind(val, name);
                }
            }
        }
    }

    private void Parse(string file, string section = "", string html = "", Dictionary<string, SerializedView> cache = null, bool loadPartials = true)
    {
        SerializedView cached = new SerializedView() { Elements = new List<ViewElement>() };
        Filename = file;
        data = new ViewData();
        Section = section;
        if (file != "")
        {
#if (!DEBUG)
        if (cache == null && ViewCache.cache != null)
        {
            cache = ViewCache.cache;
        }
#endif


            if (cache != null)
            {
                if (cache.ContainsKey(file + '/' + section) == true)
                {
                    cached = cache[file + '/' + section];
                    data = cached.Data;
                    Elements = cached.Elements;
                    Fields = cached.Fields;
                }
            }
        }

        if (cached.Elements.Count == 0)
        {
            Elements = new List<ViewElement>();

            //try loading file from disk
            if (file != "")
            {
                if (File.Exists(MapPath(file)))
                {
                    HTML = File.ReadAllText(MapPath(file));
                }
            }
            else
            {
                HTML = html;
            }
            if (HTML.Trim() == "") { return; }

            //next, find the group of code matching the view section name
            if (section != "")
            {
                //find starting tag (optionally with arguments)
                //for example: {{button (name:submit, style:outline)}}
                int[] e = new int[3];
                e[0] = HTML.IndexOf("{{" + section);
                if (e[0] >= 0)
                {
                    e[1] = HTML.IndexOf("}", e[0]);
                    if (e[1] - e[0] <= 256)
                    {
                        e[1] = HTML.IndexOf("{{/" + section + "}}", e[1]);
                    }
                    else { e[0] = -1; }

                }

                if (e[0] >= 0 & e[1] > (e[0] + section.Length + 4))
                {
                    e[2] = e[0] + 4 + section.Length;
                    HTML = HTML.Substring(e[2], e[1] - e[2]);
                }
            }

            //get view from html code
            var dirty = true;
            while (dirty == true)
            {
                dirty = false;
                var arr = HTML.Split("{{");
                var i = 0;
                var s = 0;
                var c = 0;
                var u = 0;
                var u2 = 0;
                ViewElement viewElem;

                //types of view elements

                // {{title}}                        = variable
                // {{address}} {{/address}}         = block
                // {{button "/ui/button-medium"}}   = HTML include
                // {{button "/ui/button" title:"save", onclick="do.this()"}} = HTML include with variables

                //first, load all HTML includes
                for (var x = 0; x < arr.Length; x++)
                {
                    if (x == 0 && HTML.IndexOf(arr[x]) == 0)
                    {
                        arr[x] = "{!}" + arr[x];
                    }
                    else if (arr[x].Trim() != "")
                    {
                        i = arr[x].IndexOf("}}");
                        s = arr[x].IndexOf(':');
                        u = arr[x].IndexOf('"');
                        if (i > 0 && u > 0 && u < i - 2 && (s == -1 || s > u) && loadPartials == true)
                        {
                            //read partial include & load HTML from another file
                            viewElem.Name = arr[x].Substring(0, u - 1).Trim().ToLower();
                            u2 = arr[x].IndexOf('"', u + 2);
                            var partial_path = arr[x].Substring(u + 1, u2 - u - 1);
                            if (partial_path.Length > 0) {
                                if (partial_path[0] == '/')
                                {
                                    partial_path = partial_path.Substring(1);
                                }

                                //replace pointer paths with relative paths
                                var pointer = ViewPartialPointers.Paths.Where(a => partial_path.IndexOf(a.Key) == 0).Select(p => new { p.Key, p.Value }).FirstOrDefault();
                                if (pointer != null)
                                {
                                    partial_path = partial_path.Replace(pointer.Key, pointer.Value);
                                }
                                partial_path = '/' + partial_path;


                                //load the view HTML
                                var newScaff = new View(partial_path, "", cache);

                                //check for HTML include variables
                                if (i - u2 > 0)
                                {
                                    var vars = arr[x].Substring(u2 + 1, i - (u2 + 1)).Trim();
                                    if (vars.IndexOf(":") > 0)
                                    {
                                        //HTML include variables exist
                                        try
                                        {
                                            var kv = JsonSerializer.Deserialize<Dictionary<string, string>>("{" + vars + "}");
                                            foreach (var kvp in kv)
                                            {
                                                newScaff[kvp.Key] = kvp.Value;
                                            }
                                        }
                                        catch (Exception)
                                        {
                                        }
                                    }
                                }

                                //rename child view variables, adding a prefix
                                var ht = newScaff.Render(newScaff.data, false);
                                var y = 0;
                                var prefix = viewElem.Name + "-";
                                while (y >= 0)
                                {
                                    y = ht.IndexOf("{{", y);
                                    if (y < 0) { break; }
                                    if (ht.Substring(y + 2, 1) == "/")
                                    {
                                        ht = ht.Substring(0, y + 3) + prefix + ht.Substring(y + 3);
                                    }
                                    else
                                    {
                                        ht = ht.Substring(0, y + 2) + prefix + ht.Substring(y + 2);
                                    }
                                    y += 2;
                                }

                                Partials.Add(new ViewPartial() { Name = viewElem.Name, Path = partial_path, Prefix = prefix });
                                Partials.AddRange(newScaff.Partials.Select(a =>
                                {
                                    var partial = a;
                                    partial.Prefix = prefix + partial.Prefix;
                                    return partial;
                                })
                                );
                                arr[x] = "{!}" + ht + arr[x].Substring(i + 2);
                            }
                            else
                            {
                                //partial not found
                                arr[x] = "{!}" + arr[x].Substring(i + 2);
                            }
                            
                            HTML = JoinHTML(arr);
                            dirty = true; //HTML is dirty, restart loop
                            break;
                        }
                    }

                }
                if (dirty == false)
                {
                    //next, process variables & blocks
                    for (var x = 0; x < arr.Length; x++)
                    {
                        if (x == 0 && HTML.IndexOf(arr[0].Substring(3)) == 0)//skip "{!}" using substring
                        {
                            //first element is HTML only
                            Elements.Add(new ViewElement() { Htm = arr[x].Substring(3), Name = "" });
                        }
                        else if (arr[x].Trim() != "")
                        {
                            i = arr[x].IndexOf("}}");
                            s = arr[x].IndexOf(' ');
                            c = arr[x].IndexOf(':');
                            u = arr[x].IndexOf('"');
                            viewElem = new ViewElement();
                            if (i > 0)
                            {
                                viewElem.Htm = arr[x].Substring(i + 2);

                                //get variable name
                                if (s < i && s > 0)
                                {
                                    //found space
                                    viewElem.Name = arr[x].Substring(0, s).Trim().ToLower();
                                }
                                else
                                {
                                    //found tag end
                                    viewElem.Name = arr[x].Substring(0, i).Trim().ToLower();
                                }
                                //since each variable could have the same name but different parameters,
                                //save the full name & parameters as the name
                                //viewElem.Name = arr[x].Substring(0, i);

                                if (viewElem.Name.IndexOf('/') < 0)
                                {
                                    if (Fields.ContainsKey(viewElem.Name))
                                    {
                                        //add element index to existing field
                                        var field = Fields[viewElem.Name];
                                        Fields[viewElem.Name] = field.Append(Elements.Count).ToArray();
                                    }
                                    else
                                    {
                                        //add field with element index
                                        Fields.Add(viewElem.Name, new int[] { Elements.Count });
                                    }
                                }
                                if (s < i && s > 0)
                                {
                                    //get optional variables stored within tag
                                    var vars = arr[x].Substring(s + 1, i - s - 1);
                                    //clean vars
                                    var vi = 0;
                                    var ve = 0;
                                    var inq = false;//inside quotes
                                    var vItems = new List<string>();
                                    while(vi < vars.Length)
                                    {
                                        var a = vars.Substring(vi, 1);
                                        if(a == "\"") { inq = !inq ? true : false; }
                                        if((inq == false && (a == ":" || a == ",")) || vi == vars.Length - 1)
                                        {
                                            if(vi == vars.Length - 1) { vi = vars.Length; }
                                            var r = vars.Substring(ve, vi - ve).Trim();
                                            if(r.Substring(0, 1) != "\"") { r = "\"" + r + "\""; }
                                            vItems.Add(r);
                                            ve = vi + 1;
                                        }
                                        vi++;
                                    }
                                    inq = false;
                                    vars = "";
                                    foreach(var item in vItems)
                                    {
                                        vars += item + (!inq ? ":" : ",");
                                        inq = !inq ? true : false;
                                    }
                                    if(vars[^1] == ',') { vars = vars.Substring(0, vars.Length - 1); }
                                    try
                                    {
                                        viewElem.Vars = JsonSerializer.Deserialize<Dictionary<string, string>>("{" + vars + "}");
                                    }
                                    catch (Exception ex)
                                    {
                                    }
                                }
                            }
                            else
                            {
                                viewElem.Name = "";
                                viewElem.Htm = arr[x];
                            }
                            Elements.Add(viewElem);
                        }
                    }
                }
            }
            //cache the view data
            if (cache != null)
            {
                var view = new SerializedView
                {
                    Data = data,
                    Elements = Elements,
                    Fields = Fields,
                    Partials = Partials
                };
                cache.Add(file + '/' + section, view);
            }
        }
    }

    private string JoinHTML(string[] html)
    {
        for (var x = 0; x < html.Length; x++)
        {
            if (html[x].Substring(0, 3) == "{!}")
            {
                html[x] = html[x].Substring(3);
            }
            else
            {
                html[x] = "{{" + html[x];
            }
        }
        return string.Join("", html);
    }

    private class ClosingElement
    {
        public string Name;
        public int Start;
        public int End;
        public List<bool> Show { get; set; } = new List<bool>();
    }

    public string Render()
    {
        return Render(data);
    }

    public string Render(ViewData nData, bool hideElements = true)
    {
        //deserialize list of elements since we will be manipulating the list,
        //so we don't want to permanently mutate the public elements array
        var elems = CloneElements(Elements);
        if (elems.Count > 0)
        {
            //render view with paired nData data
            var view = new StringBuilder();

            var closing = new List<ClosingElement>();
            //remove any unwanted blocks of HTML from view
            for (var x = 0; x < elems.Count; x++)
            {
                if (x < elems.Count - 1)
                {
                    for (var y = x + 1; y < elems.Count; y++)
                    {
                        //check for closing tag
                        if (elems[y].Name == "/" + elems[x].Name)
                        {
                            //add enclosed group of HTML to list for removing
                            var closed = new ClosingElement()
                            {
                                Name = elems[x].Name,
                                Start = x,
                                End = y
                            };

                            if (nData.ContainsKey(elems[x].Name) == true)
                            {
                                //check if user wants to include HTML 
                                //that is between start & closing tag  
                                if (nData[elems[x].Name, true] == true)
                                {
                                    closed.Show.Add(true);
                                }
                                else
                                {
                                    closed.Show.Add(false);
                                }
                            }
                            else
                            {
                                closed.Show.Add(false);
                            }

                            closing.Add(closed);
                            break;
                        }
                    }

                }
            }

            if (hideElements == true)
            {
                //remove all groups of HTML in list that should not be displayed
                List<int> removeIndexes = new List<int>();
                for (int x = 0; x < closing.Count; x++)
                {
                    if (closing[x].Show.FirstOrDefault() != true)
                    {
                        //add range of indexes from closing to the removeIndexes list
                        for (int y = closing[x].Start; y < closing[x].End; y++)
                        {
                            var isInList = false;
                            for (int z = 0; z < removeIndexes.Count; z++)
                            {
                                if (removeIndexes[z] == y) { isInList = true; break; }
                            }
                            if (isInList == false) { removeIndexes.Add(y); }
                        }
                    }
                }

                //physically remove HTML list items from view
                int offset = 0;
                for (int z = 0; z < removeIndexes.Count; z++)
                {
                    elems.RemoveAt(removeIndexes[z] - offset);
                    offset += 1;
                }
            }

            //finally, replace view variables with custom data
            for (var x = 0; x < elems.Count; x++)
            {
                //check if view item is an enclosing tag or just a variable
                var useView = true;
                if (elems[x].Name.IndexOf('/') < 0)
                {
                    for (int y = 0; y < closing.Count; y++)
                    {
                        if (elems[x].Name == closing[y].Name)
                        {
                            useView = false; break;
                        }
                    }
                }
                else { useView = false; }

                if ((nData.ContainsKey(elems[x].Name) == true
                || elems[x].Name.IndexOf('/') == 0) & useView == true)
                {
                    //inject string into view variable
                    var s = nData[elems[x].Name.Replace("/", "")];
                    if (string.IsNullOrEmpty(s) == true) { s = ""; }
                    view.Append(s + elems[x].Htm);
                }
                else
                {
                    //passively add htm, ignoring view variable
                    view.Append((hideElements == true ? "" : (elems[x].Name != "" ? "{{" + elems[x].Name + "}}" : "")) + elems[x].Htm);
                }
            }

            //render scaffolding as HTML string
            return view.ToString();
        }
        return "";
    }

    public string Get(string name)
    {
        var index = Elements.FindIndex(c => c.Name == name);
        if (index < 0) { return ""; }
        var part = Elements[index];
        var html = part.Htm;
        for (var x = index + 1; x < Elements.Count; x++)
        {
            part = Elements[x];
            if (part.Name == "/" + name) { break; }

            //add inner view elements
            if (part.Name.IndexOf('/') < 0)
            {
                if (data.ContainsKey(part.Name))
                {
                    if (data[part.Name, true] == true)
                    {
                        html += Get(part.Name);
                    }
                }
            }
            else
            {
                html += part.Htm;
            }

        }

        return html;
    }

    private static List<ViewElement> CloneElements(List<ViewElement> elements)
    {
        return elements.ConvertAll(a => new ViewElement()
        {
            Htm = a.Htm,
            Name = a.Name,
            Vars = a.Vars
        });
    }

    private static string MapPath(string strPath = "")
    {
        if (string.IsNullOrEmpty(ViewConfig.Path))
        {
            ViewConfig.Path = Path.GetFullPath(".");
        } 
        var path = strPath.Replace("\\", "/");
        if (path.Substring(0, 1) == "/") { path = path.Substring(1); }
        return Path.Combine(ViewConfig.Path, path);
    }
}