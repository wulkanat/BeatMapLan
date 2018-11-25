using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatMapLanguage
{
    class BmlFunction
    {
        public readonly string name;
        public readonly int pos;

        public BmlFunction(string _name, int _pos)
        {
            name = _name;
            pos = _pos;
        }
    }

    class BmlVariable
    {
        public readonly string name;
        public string value;

        public BmlVariable(string _name, string _value)
        {
            name = _name;
            value = _value;
        }
    }

    class Interpreter
    {
        //Error Codes
        public static readonly string error_variable_not_found = "VARIABLE_NOT_FOUND";
        public static readonly string error_invalid_math_operator = "INVALID_OPERATOR";
        public static readonly string error_function_not_found = "FUNCTION_NOT_FOUND";

        static readonly string no_return = "NO_RETURN";

        //BML Specific Keys
        static readonly char key_fun = '#';
        static readonly char key_comment = '§';
        static readonly char key_pattern = '!';
        static readonly char key_pattern_seperator = ':';
        static readonly char key_block_opening_bracket = '{';
        static readonly char key_block_closing_bracket = '}';
        static readonly char key_math_opening_bracket = '[';
        static readonly char key_math_closing_bracket = ']';
        static readonly char key_section_marker = '"';
        static readonly char key_variable = '_';
        static readonly char key_assignment = '=';
        static readonly char key_request_return = '?';
        static readonly char key_return_p1 = '<';
        static readonly char key_return_p2 = '-';
        static readonly char key_fun_args_opening_bracket = '(';
        static readonly char key_fun_args_closing_bracket = ')';
        static readonly char key_args_seperator = ',';

        static readonly string fun_type_function = "fun";
        static readonly string fun_type_main = "main";

        static readonly string fun_place_cube = "Cube";

        //Global Variables
        double currentOffset = 0.0;
        bool inverted = false;

        string bmlCode = "";
        int pos = 0;

        List<BmlVariable> variables = new List<BmlVariable>();
        List<BmlFunction> functions = new List<BmlFunction>();

        /*
         * Dummy function for placing cubes
         */
        void PlaceCube(double timestamp, int type, int value)
        {
        }

        /*
         * Function returns the value of the next expression or the word to the escape char
         */
        string PopNext(char escapeChar)
        {
            StringBuilder builder = new StringBuilder();

            bool variable = false;

            if (bmlCode[pos] == key_variable)
            {
                pos++;
                variable = true;
            }
            else if (bmlCode[pos] == key_math_opening_bracket)
            {
                pos++;
                return EvalMathExpression();
            }
            else if(bmlCode[pos] == key_request_return)
            {
                pos++;
                string fun_name = PopNext(' ');
                GotoNext(key_block_opening_bracket);
                return CallFunction(fun_name, PopArgs());
            }

            while (bmlCode[pos] != escapeChar)
            {
                if (bmlCode[pos] == key_section_marker)
                {
                    while (bmlCode[pos] != key_section_marker)
                    {
                        builder.Append(bmlCode[pos]);
                        pos++;
                    }
                    pos++;
                }
                else
                {
                    builder.Append(bmlCode[pos]);
                    pos++;
                }
            }
            pos++;

            if (variable)
            {
                return GetVarVal(builder.ToString());
            }
            else
            {
                return builder.ToString();
            }
        }

        /*
         * Goes to the next specified char. Because you specified the char, it already goes to the one after the specified char.
         */
        void GotoNext(char key)
        {
            while (bmlCode[pos] != key) pos++;
            pos++;
        }

        /*
         * Jumps to the next non-space char
         */
        void JumpSpaces()
        {
            while (bmlCode[pos] == ' ') pos++;
        }

        /*
         * returns all arguemts of a function as an array
         */
        string[] PopArgs()
        {
            List<string> argsList = new List<string>();

            while (bmlCode[pos] != key_fun_args_closing_bracket)
            {
                JumpSpaces();
                argsList.Add(PopNext(key_args_seperator));
            }
            pos++;

            return argsList.ToArray();
        }

        /*
         * changes the value of a variable or creates it if non-existent
         */
        void AddOrAssignVar(string name, string value)
        {
            foreach (BmlVariable variable in variables)
            {
                if (Equals(name, variable.name))
                {
                    variable.value = value;
                    return;
                }
            }

            variables.Add(new BmlVariable(name, value));
        }

        /*
         * returns the value of the variable
         */
        string GetVarVal(string name)
        {
            foreach (BmlVariable var in variables)
            {
                if (var.name == name)
                {
                    return var.value;
                }
            }

            return error_variable_not_found;
        }

        /*
         * Evaluates Math Expressions
         */
        string EvalMathExpression()
        {
            JumpSpaces();
            string varOne = PopNext(' ');
            JumpSpaces();
            string operation = PopNext(' ');
            JumpSpaces();
            string varTwo = PopNext(' ');

            GotoNext(key_math_closing_bracket);

            if (Equals(operation, "+"))
            {
                return (Double.Parse(varOne) + Double.Parse(varTwo)).ToString();
            }
            else if (Equals(operation, "-"))
            {
                return (Double.Parse(varOne) - Double.Parse(varTwo)).ToString();
            }
            else if (Equals(operation, "*"))
            {
                return (Double.Parse(varOne) * Double.Parse(varTwo)).ToString();
            }
            else if (Equals(operation, "/"))
            {
                return (Double.Parse(varOne) / Double.Parse(varTwo)).ToString();
            }
            else
            {
                return error_invalid_math_operator;
            }
        }

        string ExecuteFun(int m_pos, string[] args_names, string[] args)
        {
            int old_pos = pos;
            pos = m_pos;

            GotoNext(key_block_opening_bracket);
            JumpSpaces();

            while (bmlCode[pos] != key_block_closing_bracket)
            {
                if (bmlCode[pos] == key_comment)
                {
                    pos++;
                    GotoNext(key_comment);
                }
                else if (bmlCode[pos] == key_pattern)
                {
                    pos++;

                    string timestamp = PopNext(key_pattern_seperator);
                    string t_inverted = PopNext(key_pattern_seperator);
                    int runs = Convert.ToInt32(Double.Parse(PopNext(' ')));

                    double old_offset = currentOffset;
                    currentOffset += Double.Parse(timestamp);

                    bool old_inverted = inverted;
                    inverted = Boolean.Parse(t_inverted);

                    for (int i = 0; i < runs; i++)
                    {
                        currentOffset += Double.Parse(ExecuteFun(pos, new string[] { "p_timestamp", "p_inverted", "p_total_runs", "p_current_run_index" }, new string[] { timestamp, t_inverted, runs.ToString(), i.ToString() }));
                    }

                    GotoNext(key_block_closing_bracket);

                    currentOffset = old_offset;
                    inverted = old_inverted;
                }
                else if (bmlCode[pos] == key_return_p1 && bmlCode[pos + 1] == key_return_p2)
                {
                    pos += 2;
                    JumpSpaces();
                    string s_out = PopNext(' ');
                    pos = old_pos;
                    return s_out;
                }
                else if (bmlCode[pos] == key_variable)
                {
                    pos++;
                    string var_name = PopNext(' ');
                    JumpSpaces();
                    if (bmlCode[pos] == key_assignment)
                    {
                        pos++;
                        JumpSpaces();
                        AddOrAssignVar(var_name, PopNext(' '));
                    }
                }
                else
                {
                    string fun_call = PopNext(' ');
                    GotoNext(key_fun_args_opening_bracket);
                    CallFunction(fun_call, PopArgs());
                }

                JumpSpaces();
            }

            pos = old_pos;

            return no_return;
        }

        /*
         * Calls the specified function
         */
        string CallFunction(string name, string[] args)
        {
            if (name == fun_place_cube)
            {
                PlaceCube(Double.Parse(args[0]), int.Parse(args[1]), int.Parse(args[2]));
                return no_return;
            }
            else
            {
                //Now we have to check for custom functions

                foreach (BmlFunction fun in functions)
                {
                    if (Equals(fun.name, name))
                    {
                        int old_pos = pos;
                        pos = fun.pos;

                        string[] arg_names = PopArgs();

                        pos = old_pos;

                        return ExecuteFun(fun.pos, arg_names, args);
                    }
                }
            }

            return error_function_not_found;
        }

        /*
         * This function is responsible for managing the everything from the beginning to the end.
         */
        public void Interpret(string code)
        {
            pos = 0;
            bmlCode = code;

            while (pos < bmlCode.Length)
            {
                if (bmlCode[pos] == key_fun)
                {
                    pos++;

                    string fun_type = PopNext(' ');

                    JumpSpaces();

                    if (Equals(fun_type, fun_type_function))
                    {
                        string function_name = PopNext(' ');
                        functions.Add(new BmlFunction(function_name, pos));
                        GotoNext(key_block_closing_bracket);
                    }
                    else if (Equals(fun_type, fun_type_main))
                    {
                        ExecuteFun(pos, new string[0], new string[0]);
                        return;
                    }
                }
                else if (code[pos] == key_comment)
                {
                    pos++;
                    GotoNext(key_comment);
                }
                else
                {
                    pos++;
                }
            }
        }
    }
}