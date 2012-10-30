//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Subclass this class and define private fields for options
/// </summary>
public abstract class OptionParsing {

  /// <summary>
  /// The number of errors discovered during command-line option parsing.
  /// </summary>
  protected int errors = 0;
  private bool helpRequested;

  /// <summary>
  /// True if and only if a question mark was given as a command-line option.
  /// </summary>
  public bool HelpRequested { get { return helpRequested; } }

  /// <summary>
  /// True if and only if some command-line option caused a parsing error, or specifies an option
  /// that does not exist.
  /// </summary>
  public bool HasErrors { get { return errors > 0; } }

  /// <summary>
  /// Allows a client to signal that there is an error in the command-line options.
  /// </summary>
  public void AddError() { this.errors++; }

  /// <summary>
  /// Put this on fields if you want a more verbose help description
  /// </summary>
  protected class OptionDescription : Attribute {
    /// <summary>
    /// The text that is shown when the usage is displayed.
    /// </summary>
    readonly public string Description;
    /// <summary>
    /// Constructor for creating the information about an option.
    /// </summary>
    public OptionDescription(string s) { this.Description = s; }
    /// <summary>
    /// Indicates whether the associated option is required or not.
    /// </summary>
    public bool Required { get; set; }
    /// <summary>
    /// Indicates a short form for the option. Very useful for options
    /// whose names are reserved keywords.
    /// </summary>
    public string ShortForm { get; set; }
  }

  /// <summary>
  /// Put this on fields if you want the field to be relevant when hashing an Option object
  /// </summary>
  protected class OptionWitness : Attribute { }

  /// <summary>
  /// If a field has this attribute, then its value is inherited by all the family of analyses
  /// </summary>
  public class OptionValueOverridesFamily : Attribute { }

  /// <summary>
  /// A way to have a single option be a macro for several options.
  /// </summary>
  protected class OptionFor : Attribute {
    /// <summary>
    /// The field that this option is a macro for.
    /// </summary>
    readonly public string options;
    /// <summary>
    /// Constructor for specifying which field this is a macro option for.
    /// </summary>
    public OptionFor(string options) {
      this.options = options;
    }
  }

  /// <summary>
  /// Override and return false if options do not start with '-' or '/'
  /// </summary>
  protected virtual bool UseDashOptionPrefix { get { return true; } }

  /// <summary>
  /// This field will hold non-option arguments
  /// </summary>
  readonly List<string> generalArguments = new List<string>();

  /// <summary>
  /// The non-option arguments provided on the command line.
  /// </summary>
  public List<string> GeneralArguments { get { return this.generalArguments; } }

  #region Parsing and Reflection

  /// <summary>
  /// Called when reflection based resolution does not find option
  /// </summary>
  /// <param name="option">option name (no - or /)</param>
  /// <param name="args">all args being parsed</param>
  /// <param name="index">current index of arg</param>
  /// <param name="optionEqualsArgument">null, or the optionArgument when option was option=optionArgument</param>
  /// <returns>true if option is recognized, false otherwise</returns>
  protected virtual bool ParseUnknown(string option, string[] args, ref int index, string optionEqualsArgument) {
    return false;
  }

  /// <summary>
  /// Main method called by a client to process the command-line options.
  /// </summary>
  public void Parse(string[] args) {
    var requiredOptions = GatherRequiredOptions();
    int index = 0;
    while (index < args.Length) {
      string arg = args[index];

      if (arg == "") { index++; continue; }

      // We use this letters to "comment" parameters - it is just for convenience
      if (arg[0] == '+' || arg[0] == '!') {
        index++;
        continue;
      }

      if (!UseDashOptionPrefix || arg[0] == '/' || arg[0] == '-') {
        if (UseDashOptionPrefix) {
          arg = arg.Remove(0, 1);
        }
        if (arg == "?") {
          this.helpRequested = true;
          this.errors++;
          index++;
          continue;
        }
        string equalArgument = null;
        int equalIndex = arg.IndexOf(':');
        if (equalIndex <= 0) {
          equalIndex = arg.IndexOf('=');
          if (equalIndex < 0) // Also add '!' as synonim for '=', as cmd.exe performs fuzzy things with '='
            equalIndex = arg.IndexOf('!');
        }
        if (equalIndex > 0) {
          equalArgument = arg.Substring(equalIndex + 1);
          arg = arg.Substring(0, equalIndex);
        }

        bool optionOK = this.FindOptionByReflection(arg, args, ref index, equalArgument, ref requiredOptions);
        if (!optionOK) {
          optionOK = this.ParseUnknown(arg, args, ref index, equalArgument);
          if (!optionOK) {
            Console.WriteLine("Unknown option '{0}'", arg);
            errors++;
          }
        }
      } else {
        this.generalArguments.Add(arg);
      }

      index++;
    }
    if (!helpRequested) CheckRequiresOptions(requiredOptions);
  }

  private void CheckRequiresOptions(IEnumerable<string> requiredOptions) {
    foreach (var missed in requiredOptions) {
      Console.WriteLine("Required option '-{0}' was not given.", missed);
      errors++;
    }
  }

  private IList<string> GatherRequiredOptions() {
    List<string> result = new List<string>();

    foreach (var field in this.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)) {
      var options = field.GetCustomAttributes(typeof(OptionDescription), false);
      foreach (OptionDescription attr in options) {
        if (attr.Required) {
          result.Add(field.Name.ToLowerInvariant());
        }
      }
    }
    return result;
  }

  private bool ParseValue<T>(Converter<string, T> parser, string equalArgument, string[] args, ref int index, ref object result) {
    if (equalArgument == null) {
      if (index + 1 < args.Length) {
        equalArgument = args[++index];
      }
    }
    bool success = false;
    if (equalArgument != null) {
      try {
        result = parser(equalArgument);
        success = true;
      } catch {
      }
    }
    return success;
  }

  private bool ParseValue<T>(Converter<string, T> parser, string argument, ref object result) {
    bool success = false;
    if (argument != null) {
      try {
        result = parser(argument);
        success = true;
      } catch {
      }
    }
    return success;
  }

  private object ParseValue(Type type, string argument, string option) {
    object result = null;
    if (type == typeof(bool)) {
      if (argument != null) {
        if (!ParseValue<bool>(Boolean.Parse, argument, ref result)) {
          // Allow "+/-" to turn on/off boolean options
          if (argument.Equals("-")) {
            result = false;
          } else if (argument.Equals("+")) {
            result = true;
          } else {
            Console.WriteLine("option -{0} requires a bool argument", option);
            this.errors++;
          }
        }
      } else {
        result = true;
      }
    } else if (type == typeof(string)) {
      if (!ParseValue<string>(Identity, argument, ref result)) {
        Console.WriteLine("option -{0} requires a string argument", option);
        this.errors++;
      }
    } else if (type == typeof(int)) {
      if (!ParseValue<int>(Int32.Parse, argument, ref result)) {
        Console.WriteLine("option -{0} requires an int argument", option);
        this.errors++;
      }
    } else if (type.IsEnum) {
      if (!ParseValue<object>(ParseEnum(type), argument, ref result)) {
        Console.WriteLine("option -{0} expects one of", option);
        this.errors++;
        foreach (System.Reflection.FieldInfo enumConstant in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)) {
          if (enumConstant.IsLiteral) {
            Console.WriteLine("  {0}", enumConstant.Name);
          }
        }
      }
    }
    return result;
  }

  string AdvanceArgumentIfNoExplicitArg(Type type, string explicitArg, string[] args, ref int index) {
    if (explicitArg != null) return explicitArg;
    if (type == typeof(bool)) {
      // bool args don't grab the next arg
      return null;
    }
    if (index + 1 < args.Length) {
      return args[++index];
    }
    return null;
  }

  private bool FindOptionByReflection(string arg, string[] args, ref int index, string explicitArgument, ref IList<string> requiredOptions) {
    System.Reflection.FieldInfo fi = this.GetType().GetField(arg, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
    if (fi != null) {
      requiredOptions.Remove(arg.ToLowerInvariant());
      return ProcessOptionWithMatchingField(arg, args, ref index, explicitArgument, ref fi);
    } else {
      // derived options
      fi = this.GetType().GetField(arg, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
      if (fi != null) {
        object derived = fi.GetValue(this);

        if (derived is string) {
          this.Parse(((string)derived).Split(' '));
          requiredOptions.Remove(arg.ToLowerInvariant());
          return true;
        }
      }

      // Try to see if the arg matches any ShortForm of an option
      var allFields = this.GetType().GetFields();
      System.Reflection.FieldInfo matchingField = null;
      foreach (var f in allFields) {
        matchingField = f;
        var options = matchingField.GetCustomAttributes(typeof(OptionDescription), false);
        foreach (OptionDescription attr in options) {
          if (attr.ShortForm != null) {
            var lower1 = attr.ShortForm.ToLowerInvariant();
            var lower2 = arg.ToLowerInvariant();
            if (lower1.Equals(lower2)) {
              requiredOptions.Remove(matchingField.Name.ToLowerInvariant());
              return ProcessOptionWithMatchingField(arg, args, ref index, explicitArgument, ref matchingField);
            }
          }
        }
      }
    }
    return false;
  }

  private bool ProcessOptionWithMatchingField(string arg, string[] args, ref int index, string explicitArgument, ref System.Reflection.FieldInfo fi) {
    Type type = fi.FieldType;
    bool isList = false;
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
      isList = true;
      type = type.GetGenericArguments()[0];
    }
    if (isList && explicitArgument == "") {
      // way to set list to empty
      System.Collections.IList listhandle = (System.Collections.IList)fi.GetValue(this);
      listhandle.Clear();
      return true;
    }
    string argument = AdvanceArgumentIfNoExplicitArg(type, explicitArgument, args, ref index);
    if (isList) {
      string[] listargs = argument.Split(';');
      for (int i = 0; i < listargs.Length; i++) {
        bool remove = listargs[i][0] == '!';
        string listarg = remove ? listargs[i].Substring(1) : listargs[i];
        object value = ParseValue(type, listarg, arg);
        if (value != null) {
          if (remove) {
            this.GetListField(fi).Remove(value);
          } else {
            this.GetListField(fi).Add(value);
          }
        }
      }
    } else {
      object value = ParseValue(type, argument, arg);
      if (value != null) {
        fi.SetValue(this, value);

        string argname;
        if (value is Int32 && HasOptionForAttribute(fi, out argname)) {
          this.Parse(DerivedOptionFor(argname, (Int32)value).Split(' '));
        }

      }
    }
    return true;
  }

  private bool HasOptionForAttribute(System.Reflection.FieldInfo fi, out string argname) {
    var options = fi.GetCustomAttributes(typeof(OptionFor), true);
    if (options != null && options.Length == 1) {
      argname = ((OptionFor)options[0]).options;
      return true;
    }

    argname = null;
    return false;
  }

  /// <summary>
  /// For the given field, returns the derived option that is indexed by
  /// option in the list of derived options.
  /// </summary>
  protected string DerivedOptionFor(string fieldWithOptions, int option) {
    string[] options;
    if (TryGetOptions(fieldWithOptions, out options)) {
      if (option < 0 || option >= options.Length) {
        return "";
      }

      return options[option];
    }

    return "";
  }

  /// <summary>
  /// Returns the options associated with the field, specified as a string.
  /// If there are none, options is set to null and false is returned.
  /// </summary>
  protected bool TryGetOptions(string fieldWithOptions, out string[] options) {
    var fi = this.GetType().GetField(fieldWithOptions,
      System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public);

    if (fi != null) {
      var obj = fi.GetValue(this);

      if (obj is string[]) {
        options = (string[])obj;
        return true;
      }
    }

    options = null;
    return false;
  }

  /// <summary>
  /// Use this 
  /// </summary>
  public long GetCheckCode() {
    var res = 0L;
    foreach (var f in this.GetType().GetFields()) {
      foreach (var a in f.GetCustomAttributes(true)) {
        if (a is OptionWitness) {
          res += (f.GetValue(this).GetHashCode()) * f.GetHashCode();

          break;
        }
      }
    }

    return res;
  }

  string Identity(string s) { return s; }

  Converter<string, object> ParseEnum(Type enumType) {
    return delegate(string s) { return Enum.Parse(enumType, s, true); };
  }

  System.Collections.IList GetListField(System.Reflection.FieldInfo fi) {
    object obj = fi.GetValue(this);
    if (obj != null) { return (System.Collections.IList)obj; }
    System.Collections.IList result = (System.Collections.IList)fi.FieldType.GetConstructor(new Type[] { }).Invoke(new object[] { });
    fi.SetValue(this, result);
    return result;
  }

  /// <summary>
  /// Writes all of the options out to the console.
  /// </summary>
  public void PrintOptions(string indent) {
    foreach (System.Reflection.FieldInfo f in this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
      System.Type opttype = f.FieldType;
      bool isList;
      if (opttype.IsGenericType && opttype.GetGenericTypeDefinition() == typeof(List<>)) {
        opttype = opttype.GetGenericArguments()[0];
        isList = true;
      } else {
        isList = false;
      }
      string description = GetOptionAttribute(f);
      string option = null;
      if (opttype == typeof(bool)) {
        if (!isList && f.GetValue(this).Equals(true)) {
          option = String.Format("{0} (default=true)", f.Name);
        } else {
          option = f.Name;
        }
      } else if (opttype == typeof(string)) {
        if (!f.IsLiteral) {
          object defaultValue = f.GetValue(this);
          if (!isList && defaultValue != null) {
            option = String.Format("{0} <string-arg> (default={1})", f.Name, defaultValue);
          } else {
            option = String.Format("{0} <string-arg>", f.Name);
          }
        }
      } else if (opttype == typeof(int)) {
        if (!isList) {
          option = String.Format("{0} <int-arg> (default={1})", f.Name, f.GetValue(this));
        } else {
          option = String.Format("{0} <int-arg>", f.Name);
        }
      } else if (opttype.IsEnum) {
        StringBuilder sb = new StringBuilder();
        sb.Append(f.Name).Append(" (");
        bool first = true;
        foreach (System.Reflection.FieldInfo enumConstant in opttype.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)) {
          if (enumConstant.IsLiteral) {
            if (!first) {
              if (isList) {
                sb.Append(" + ");
              } else {
                sb.Append(" | ");
              }
            } else {
              first = false;
            }
            sb.Append(enumConstant.Name);
          }
        }
        sb.Append(") ");
        if (!isList) {
          sb.AppendFormat("(default={0})", f.GetValue(this));
        } else {
          sb.Append("(default=");
          bool first2 = true;
          foreach (object eval in (System.Collections.IEnumerable)f.GetValue(this)) {
            if (!first2) {
              sb.Append(',');
            } else {
              first2 = false;
            }
            sb.Append(eval.ToString());
          }
          sb.Append(')');
        }
        option = sb.ToString();
      }
      if (option != null) {
        Console.Write("{1}   -{0,-30}", option, indent);

        if (description != null) {
          Console.WriteLine(" : {0}", description);
        } else {
          Console.WriteLine();
        }
      }
    }
  }

  /// <summary>
  /// Prints all of the derived options to the console.
  /// </summary>
  public void PrintDerivedOptions(string indent) {
    foreach (System.Reflection.FieldInfo f in this.GetType().GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)) {
      if (f.IsLiteral) {
        Console.WriteLine("{2}   -{0} is '{1}'", f.Name, f.GetValue(this), indent);
      }
    }
  }

  private string GetOptionAttribute(System.Reflection.FieldInfo f) {
    object[] attrs = f.GetCustomAttributes(typeof(OptionDescription), true);
    if (attrs != null && attrs.Length == 1) {
      StringBuilder result = new StringBuilder();
      OptionDescription descr = (OptionDescription)attrs[0];
      if (descr.Required) {
        result.Append("(required) ");
      }
      result.Append(descr.Description);
      if (descr.ShortForm != null) {
        result.Append("[short form: " + descr.ShortForm + "]");
      }
      object[] optionsFor = f.GetCustomAttributes(typeof(OptionFor), true);
      string[] options;

      if (optionsFor != null && optionsFor.Length == 1 && TryGetOptions(((OptionFor)optionsFor[0]).options, out options)) {
        result.AppendLine(Environment.NewLine + "Detailed explanation:");
        for (int i = 0; i < options.Length; i++) {
          result.Append(string.Format("{0} : {1}" + Environment.NewLine, i, options[i]));
        }
      }

      return result.ToString();
    }
    return null;
  }


  #endregion

}
