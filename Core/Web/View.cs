using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.ComponentModel;

[Serializable]
public struct ViewElement
{
    public string Name { get; set; }
    public string Html { get; set; }
    public Dictionary<string, string> Vars { get; set; }
    public string Var { get; set; }
    public bool isBlock { get; set; }
    public int? blockEnd { get; set; }
}

public static class ViewCache
{
    public static Dictionary<string, View> Cache { get; set; } = new Dictionary<string, View>();

    public static void Remove(string file, string section = "")
    {
        if (Cache.ContainsKey(file + '/' + section) == true)
        {
            Cache.Remove(file + '/' + section);
        }
    }

    public static void Clear()
    {
        Cache.Clear();
    }
}

public class ViewChild
{
    public ViewDictionary Data { get; set; }
    public Dictionary<string, int[]> Fields = new Dictionary<string, int[]>();
    public View Parent { get; set; }

    public ViewChild(View parent, string id)
    {
        Parent = parent;
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
/// Allow developers to create pointers so that the mustache variable can use a 
/// pointer path instead of the actual relative path to a partial view file
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
            try
            {
                _data[key] = value;
            }
            catch (Exception)
            {
                _data.Add(key, value);
            }
            
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

    public ViewData Data;
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
        else if(options.File != "")
        {
            Parse(options.File, options.Section, "");
        }
    }

    /// <summary>
    /// Use a template file to bind data and replace mustache variables with data, e.g. {{my-name}} is replaced with value of View["my-name"]
    /// </summary>
    /// <param name="file">relative path to the template file</param>
    /// <param name="cache">Dictionary object used to save cached, parsed template to</param>
    public View(string file, Dictionary<string, View> cache = null)
    {
        Parse(file, "", "", cache);
    }

    /// <summary>
    /// Use a template file to bind data and replace mustache variables with data, e.g. {{my-name}} is replaced with value of View["my-name"]
    /// </summary>
    /// <param name="file">relative path to the template file</param>
    /// <param name="section">section name within the template file to load, e.g. {{my-section}} ... {{/my-section}}</param>
    /// <param name="cache">Dictionary object used to save cached, parsed template to</param>
    public View(string file, string section, Dictionary<string, View> cache = null)
    {
        Parse(file, section.ToLower(), "", cache);
    }

    /// <summary>
    /// Most common way to load a view as it will access the ViewCache first, then generate a new View object if it is not cached
    /// </summary>
    /// <param name="file">relative path to your file</param>
    /// <param name="section">The name of a mustache block found inside the file</param>
    /// <returns>Either the cached View or a new View</returns>
    public static View Load(string file, string section = "")
    {
        return new View(file, section);
    }

    public string this[string key]
    {
        get
        {
            try { 
                return Data[key.ToLower()];
            }
            catch (Exception)
            {
                Data.Add(key.ToLower(), "");
                return "";
            }
        }
        set
        {
            try
            {
                Data[key.ToLower()] = value;
            }
            catch (Exception)
            {
                Data.Add(key.ToLower(), value);
            }
            
        }
    }

    public bool ContainsKey(string key)
    {
        return Data.ContainsKey(key.ToLower());
    }

    public void Show(string blockKey)
    {
        Data[blockKey.ToLower(), true] = true;
    }

    /// <summary>
    /// Clears any bound data, which is useful when reusing the same View object in a loop
    /// </summary>
    public void Clear()
    {
        Data = new ViewData();
    }

    /// <summary>
    /// If the Element item at the given index is a mustache block, return contents of entire block
    /// </summary>
    /// <param name="ElementIndex"></param>
    /// <returns>Index of Elements item</returns>
    public string GetBlock(int ElementIndex)
    {
        if(ElementIndex < 0) { return ""; }
        var html = new StringBuilder();
        var elem = Elements[ElementIndex];
        if (!elem.isBlock) { return ""; }
        html.Append(elem.Html);
        var i = ElementIndex + 1;
        while (i < Elements.Count)
        {
            var el = Elements[i];
            if(el.Name == "/" + elem.Name) { break; }
            html.Append("{{" + el.Name + (el.Var != null && el.Var != "" ? " " + el.Var : "") + "}}" + el.Html);
            i++;
        }
        return html.ToString();
    }

    /// <summary>
    /// If the Element is a mustache block, return contents of entire block
    /// </summary>
    /// <param name="Element"></param>
    /// <returns></returns>
    public string GetBlock(ViewElement Element)
    {
        var index = Elements.IndexOf(Element);
        if(index >= 0)
        {
            return GetBlock(index);
        }
        return "";
    }

    /// <summary>
    /// Binds an object to the view template. Use e.g. {{myprop}} or {{myobj.myprop}} to represent 
    /// object fields & properties in template
    /// </summary>
    /// <param name="obj">The object that you wish to bind to mustache variables within the View contents</param>
    /// <param name="name">Specify the root object name found within your mustache variables (e.g. {{myobj.myprop}} would use "myobj" as the root)</param>
    /// <param name="dtFormat">Date/Time string formatting to use for DateTime objects</param>
    public void Bind(object obj, string name = "", string dtFormat = "M/dd/yyyy h:mm tt")
    {
        if (obj != null)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(obj))
            {
                object val = property.GetValue(obj);
                var varname = (name != "" ? name + "." : "") + property.Name.ToLower();
                if (val == null)
                {
                    Data[varname] = "";
                }
                else if (val is string || val is int || val is long || val is double || val is decimal || val is short)
                {
                    //add property value to dictionary
                    Data[varname] = val.ToString();
                }
                else if (val is bool)
                {
                    Data[varname] = (bool)val == true ? "1" : "0";
                }
                else if (val is DateTime)
                {
                    Data[varname] = ((DateTime)val).ToString(dtFormat);
                }
                else if (val is object)
                {
                    //recurse child object for properties
                    Bind(val, varname);
                }
            }
        }
    }

    private void Parse(string file, string section = "", string html = "", Dictionary<string, View> cache = null, bool loadPartials = true)
    {
        Filename = file;
        Data = new ViewData();
        Section = section;
        if (file != "")
        {
#if (!DEBUG)
        if (cache == null && ViewCache.Cache != null)
        {
            cache = ViewCache.Cache;
        }
#endif

            if (cache != null)
            {
                if (cache.ContainsKey(file + '/' + section) == true)
                {
                    var cached = cache[file + '/' + section];
                    Elements = cached.Elements;
                    Fields = cached.Fields;
                    Partials = cached.Partials;
                    return;
                }
            }
        }

        //was NOT able to retrieve cached object
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
        string[] arr;
        var i = 0;
        var s = 0;
        var c = 0;
        var u = 0;
        var u2 = 0;
        ViewElement viewElem;
        while (dirty == true)
        {
            dirty = false;
            arr = HTML.Split("{{");
            i = 0;
            s = 0;
            c = 0;
            u = 0;
            u2 = 0;
            viewElem = new ViewElement();

            //types of view elements

                // {{title}}                        = variable
                // {{address}} {{/address}}         = block
                // {{button "/ui/button-medium"}}   = HTML include
                // {{button "/ui/button" title:"save", onclick="do.this()"}} = HTML include with properties
                // {{page-list path:"blog", length:"5"}} = HTML variable with properties

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
                            var ht = newScaff.Render(newScaff.Data, false);
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
                        Elements.Add(new ViewElement() { Html = arr[x].Substring(3), Name = "" });
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
                            viewElem.Html = arr[x].Substring(i + 2);

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

                            if (!viewElem.Name.Contains('/'))
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
                                //check if view element is a block
                                for(var y = x + 1; y < arr.Length; y++)
                                {
                                    if(arr[y].IndexOf("/" + viewElem.Name + "}}") == 0)
                                    {
                                        viewElem.isBlock = true;
                                        viewElem.blockEnd = y;
                                        break;
                                    }
                                }
                            }
                            if (s < i && s > 0)
                            {
                                //get optional variables stored within tag
                                var vars = arr[x].Substring(s + 1, i - s - 1);
                                viewElem.Var = vars.Trim();
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
                                catch (Exception){}
                            }
                        }
                        else
                        {
                            viewElem.Name = "";
                            viewElem.Html = arr[x];
                        }
                        Elements.Add(viewElem);
                    }
                }
            }
        }
        //cache the view data
        if (cache != null && !cache.ContainsKey(file + "/" + section))
        {
            cache.Add(file + '/' + section, this);
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

    public string Render()
    {
        return Render(Data);
    }

    public string Render(ViewData nData, bool hideElements = true)
    {
        if (Elements.Count == 0) { return ""; }
        var html = new StringBuilder();
        for (var x = 0; x < Elements.Count; x++)
        {
            //check if view item is an enclosing tag or just a variable
            if (Elements[x].isBlock == false && nData.ContainsKey(Elements[x].Name))
            {
                //inject string into view variable
                html.Append(nData[Elements[x].Name] + Elements[x].Html);
            }
            else
            {
                if (hideElements == true && Elements[x].blockEnd != null && !(Elements[x].isBlock && Elements[x].Name.IndexOf('/') < 0 && nData.ContainsKey(Elements[x].Name) && nData[Elements[x].Name] == "True"))
                {
                    //skip elements if user decides not to show a block
                    x = Elements[x].blockEnd.Value - 1;
                }
                else if(hideElements == false && Elements[x].Name != "")
                {
                    //passively add htm, ignoring view variable
                    html.Append("{{" + Elements[x].Name + 
                        (!string.IsNullOrEmpty(Elements[x].Var) ? " " + Elements[x].Var : "") + 
                        "}}" + Elements[x].Html);
                }
                else
                {
                    html.Append(Elements[x].Html);
                }
            }
        }
        //render scaffolding as HTML string
        return html.ToString();
    }

    public string Get(string name)
    {
        var index = Elements.FindIndex(c => c.Name == name);
        if (index < 0) { return ""; }
        var part = Elements[index];
        var html = part.Html;
        for (var x = index + 1; x < Elements.Count; x++)
        {
            part = Elements[x];
            if (part.Name == "/" + name) { break; }

            //add inner view elements
            if (part.Name.IndexOf('/') < 0)
            {
                try
                {
                    if (Data[part.Name, true] == true)
                    {
                        html += Get(part.Name);
                    }
                }
                catch (Exception) { }
            }
            else
            {
                html += part.Html;
            }

        }

        return html;
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