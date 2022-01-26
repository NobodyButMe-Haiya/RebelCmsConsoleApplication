using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace RebelCmsConsoleApplication
{
    namespace RebelCmsGenerator
    {
        internal class CodeGenerator
        {
            private const string DefaultDatabase = "rebelcms";
            private readonly string _connection;

            private enum TextCase
            {
                LcWords,
                UcWords
            }

            public class DescribeTableModel
            {
                public string? KeyValue { get; init; }
                public string? FieldValue { get; init; }
                public string? TypeValue { get; init; }
            }

            public CodeGenerator(string connection1)
            {
                _connection = connection1;
            }

            private MySqlConnection GetConnection()
            {
                return new MySqlConnection(_connection);
            }

            private static IEnumerable<string> GetStringDataType()
            {
                return new List<string> {"char", "varchar", "text", "tinytext", "mediumtext", "longtext"};
            }

            private static IEnumerable<string> GetBlobType()
            {
                return new List<string> {"tinyblob", "mediumblob", "blob", "longblob"};
            }

            private static IEnumerable<string> GetNumberDataType()
            {
                return new List<string>
                {
                    "tinyinit", "bool", "boolean", "smallint", "int", "integer", "year", "INT", "YEAR", "SMALLINT",
                    "BOOL", "BOOLEAN"
                };
            }

            private static IEnumerable<string> GetNumberDotDataType()
            {
                return new List<string> {"decimal", "float", "double"};
            }

            private static IEnumerable<string> GetDoubleDataType()
            {
                return new List<string> {"float", "double"};
            }

            private static IEnumerable<string> GetDateDataType()
            {
                return new List<string> {"date", "datetime", "timestamp", "time"};
            }

            private static IEnumerable<string> GetHiddenField()
            {
                return new List<string> {"tenantId", "isDelete", "executeBy"};
            }

            public List<DescribeTableModel> GetTableStructure(string tableName)
            {
                using var connection = GetConnection();
                connection.Open();

                List<DescribeTableModel> describeTableModels = new();
                var sql = $@"DESCRIBE  `{tableName}` ";
                MySqlCommand command = new(sql, connection);
                try
                {
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            describeTableModels.Add(new DescribeTableModel
                            {
                                KeyValue = reader["Key"].ToString(),
                                FieldValue = reader["Field"].ToString(),
                                TypeValue = reader["Type"].ToString()
                            });
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No Record");
                    }
                }
                catch (MySqlException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    command.Dispose();
                }

                return describeTableModels;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="module"></param>
            /// <param name="tableName"></param>
            /// <param name="readOnly"></param>
            /// <param name="detailTableName">Optional future .</param>
            /// <returns></returns>
            public string GenerateModel(string module, string tableName, bool readOnly = false,
                string detailTableName = "")
            {
                //     var ucTableName = GetStringNoUnderScore(tableName, (int) TextCase.UcWords);
                //    var lcTableName = GetStringNoUnderScore(tableName, (int) TextCase.LcWords);
                var describeTableModels = GetTableStructure(tableName);

                List<DescribeTableModel> describeTableDetailModels = new();
                if (string.IsNullOrEmpty(detailTableName))
                {
                    describeTableDetailModels = GetTableStructure(detailTableName);
                }

                StringBuilder template = new();
                template.AppendLine($"namespace RebelCmsTemplate.Models.{UpperCaseFirst(module)};");
                // if got detail table should at least have partial class to bring information if wanted to grid info
                if (readOnly)
                {
                    template.AppendLine("public partial class " +
                                        GetStringNoUnderScore(tableName, (int) TextCase.UcWords) + "Model");
                }
                else
                {
                    template.AppendLine("public class " + GetStringNoUnderScore(tableName, (int) TextCase.UcWords) +
                                        "Model");
                }

                template.AppendLine("{");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (key.Equals("PRI") || key.Equals("MUL"))
                    {
                        template.AppendLine("\tpublic int " + UpperCaseFirst(field.Replace("Id", "Key")) +
                                            " { get; init; } ");
                    }
                    else
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\tpublic int " + UpperCaseFirst(field) + " { get; init; } ");
                        }
                        else if (type.Contains("decimal"))
                        {
                            template.AppendLine("\tpublic decimal " + UpperCaseFirst(field) + " { get; init; } ");
                        }
                        else if (GetDoubleDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\tpublic double " + UpperCaseFirst(field) + " { get; init; } ");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            switch (type)
                            {
                                case "datetime":
                                    template.AppendLine("\tpublic DateTime? " + UpperCaseFirst(field) +
                                                        "  { get; init; } ");
                                    break;
                                case "date":
                                    template.AppendLine("\tpublic DateOnly? " + UpperCaseFirst(field) +
                                                        " { get; init; } ");
                                    break;
                                case "time":
                                    template.AppendLine("\tpublic TimeOnly? " + UpperCaseFirst(field) +
                                                        " { get; init; } ");
                                    break;
                                case "year":
                                    template.AppendLine("\tpublic int " + UpperCaseFirst(field) + " { get; init; } ");
                                    break;
                                default:
                                    template.AppendLine(
                                        "\tpublic string? " + UpperCaseFirst(field) + " { get; init; } ");
                                    break;
                            }
                        }
                        else if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\tpublic byte[]?  " + UpperCaseFirst(field) + " { get; set; } ");
                            template.AppendLine("\tpublic string?  " + UpperCaseFirst(field) +
                                                "Base64String { get; set; } ");
                        }
                        else
                        {
                            template.AppendLine("\tpublic string? " + UpperCaseFirst(field) + " { get; init; } ");
                        }
                    }
                }

                // this is more on foreign key field name
                template.AppendLine("}");
                if (!readOnly) return template.ToString();
                {
                    template.AppendLine("public partial class " +
                                        GetStringNoUnderScore(tableName, (int) TextCase.UcWords) + "Model");
                    template.AppendLine("{");

                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (key.Equals("MUL"))
                        {
                            // get the foreign key name
                            template.AppendLine("\tpublic string? " +
                                                UpperCaseFirst(
                                                    GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                " { get; init; } ");
                        }
                    }
                    // there may be optional detail master detail so

                    if (!string.IsNullOrEmpty(detailTableName))
                    {
                        foreach (var describeTableModel in describeTableDetailModels)
                        {
                            var key = string.Empty;
                            var field = string.Empty;
                            if (describeTableModel.KeyValue != null)
                            {
                                key = describeTableModel.KeyValue;
                            }

                            if (describeTableModel.FieldValue != null)
                            {
                                field = describeTableModel.FieldValue;
                            }

                            if (key.Equals("MUL"))
                            {
                                // get the foreign key name
                                template.AppendLine("\tpublic string? " +
                                                    UpperCaseFirst(
                                                        GetLabelForComboBoxForGridOrOption(detailTableName,
                                                            field)) + " { get; init; } ");
                            }
                        }
                    }

                    // a list master detail . the diff we don't loop and check .Separate query
                    if (!string.IsNullOrEmpty(detailTableName))
                    {
                        template.AppendLine("\tpublic List<" + UpperCaseFirst(detailTableName) +
                                            "Model>? Data { get; set; } ");
                    }

                    template.AppendLine("}");
                }


                return template.ToString();
            }

            public string GenerateController(string module, string tableName, string tableNameDetail = "")
            {
                var ucTableName = GetStringNoUnderScore(tableName, (int) TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int) TextCase.LcWords);
                var describeTableModels = GetTableStructure(tableName);
                var fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();

                StringBuilder template = new();
                StringBuilder createModelString = new();
                StringBuilder updateModelString = new();
                var counter = 1;
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                    {
                        name = fieldName;
                    }

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            name = name.Replace("Id", "Key");
                        }

                        if (counter == fieldNameList.Count)
                        {
                            updateModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)}");
                            createModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)}");
                        }
                        else
                        {
                            if (!name.Equals(lcTableName + "Key"))
                            {
                                createModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)},");
                            }

                            updateModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)},");
                        }
                    }

                    counter++;
                }

                template.AppendLine("using Microsoft.AspNetCore.Mvc;");
                template.AppendLine("using RebelCmsTemplate.Enum;");
                template.AppendLine($"using RebelCmsTemplate.Models.{module};");
                template.AppendLine($"using RebelCmsTemplate.Repository.{module};");
                template.AppendLine("using RebelCmsTemplate.Util;");

                template.AppendLine($"namespace RebelCmsTemplate.Controllers.{module};");
                template.AppendLine($"[Route(\"api/{module.ToLower()}/[controller]\")]");
                template.AppendLine("[ApiController]");
                template.AppendLine("public class " + ucTableName + "Controller : Controller {");

                template.AppendLine(" private readonly IHttpContextAccessor _httpContextAccessor;");
                template.AppendLine(" private readonly RenderViewToStringUtil _renderViewToStringUtil;");
                template.AppendLine(" public " + ucTableName +
                                    "Controller(RenderViewToStringUtil renderViewToStringUtil, IHttpContextAccessor httpContextAccessor)");
                template.AppendLine(" {");
                template.AppendLine("  _renderViewToStringUtil = renderViewToStringUtil;");
                template.AppendLine("  _httpContextAccessor = httpContextAccessor;");
                template.AppendLine(" }");
                template.AppendLine(" [HttpGet]");
                template.AppendLine(" public async Task<IActionResult> Get()");
                template.AppendLine(" {");
                template.AppendLine("   SharedUtil sharedUtils = new(_httpContextAccessor);");
                template.AppendLine("   if (sharedUtils.GetTenantId() == 0 || sharedUtils.GetTenantId().Equals(null))");
                template.AppendLine("   {");
                template.AppendLine("    const string? templatePath = \"~/Views/Error/403.cshtml\";");
                template.AppendLine(
                    "    var page = await _renderViewToStringUtil.RenderViewToStringAsync(ControllerContext, templatePath);");
                template.AppendLine("    return Ok(page);");
                template.AppendLine("   }");
                template.AppendLine($"   {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"   var content = {lcTableName}Repository.GetExcel();");

                Random random = new();
                var fileName = lcTableName + random.Next(1, 100);

                template.AppendLine(
                    $"   return File(content,\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\",\"{fileName}.xlsx\");");
                template.AppendLine("  }");
                template.AppendLine("  [HttpPost]");

                var imageUpload = false;
                List<string> imageFileName = new();

                foreach (var describeTableModel in describeTableModels)
                {
                    var field = string.Empty;
                    var type = string.Empty;

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (!GetBlobType().Any(x => type.Contains(x))) continue;
                    imageUpload = true;
                    imageFileName.Add(UpperCaseFirst(field));
                    break;
                }

                if (imageFileName.Count > 0)
                {
                    imageUpload = true;
                }

                template.AppendLine(!imageUpload
                    ? "public ActionResult Post()"
                    : "\tpublic async Task<ActionResult> Post()");

                template.AppendLine("  {");
                template.AppendLine("\tvar status = false;");
                template.AppendLine("\tvar mode = Request.Form[\"mode\"];");
                template.AppendLine("\tvar leafCheckKey = Convert.ToInt32(Request.Form[\"leafCheckKey\"]);");
                template.AppendLine(
                    $"           {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine("\tSharedUtil sharedUtil = new(_httpContextAccessor);");
                template.AppendLine("\tCheckAccessUtil checkAccessUtil = new (_httpContextAccessor);");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }


                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (key.Equals("PRI") || key.Equals("MUL"))
                        {
                            field = field.Replace("Id", "Key");
                        }

                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine(
                                $"\tvar {field} =  !string.IsNullOrEmpty(Request.Form[\"{field}\"])?Convert.ToInt32(Request.Form[\"{field}\"]):0;");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine($"\tDateTime {field} = DateTime.MinValue;");
                                template.AppendLine($"\tif (!string.IsNullOrEmpty(Request.Form[\"{field}\"]))");
                                template.AppendLine("\t{");
                                template.AppendLine($"\tvar test = Request.Form[\"{field}\"].ToString().Split(\"T\");");
                                template.AppendLine("\tvar dateString = test[0].Split(\"-\");");
                                template.AppendLine("\tvar timeString = test[1].Split(\":\");");

                                template.AppendLine("\tvar year = Convert.ToInt32(dateString[0]);");
                                template.AppendLine("\tvar month = Convert.ToInt32(dateString[1]);");
                                template.AppendLine("\tvar day = Convert.ToInt32(dateString[2]);");

                                template.AppendLine("\tvar hour = Convert.ToInt32(timeString[0].ToString());");
                                template.AppendLine("\tvar minute = Convert.ToInt32(timeString[1].ToString());");

                                template.AppendLine($"\t{field} = new(year, month, day, hour, minute, 0);");
                                template.AppendLine("\t}");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine($"\tvar {field} = DateOnly.FromDateTime(DateTime.Now);");
                                template.AppendLine($"\tif (!string.IsNullOrEmpty(Request.Form[\"{field}\"]))");
                                template.AppendLine("\t{");
                                template.AppendLine(
                                    $"\tvar dateString = Request.Form[\"{field}\"].ToString().Split(\"-\");");
                                template.AppendLine(
                                    $"\t{field} = new DateOnly(Convert.ToInt32(dateString[0]), Convert.ToInt32(dateString[1]), Convert.ToInt32(dateString[2]));");
                                template.AppendLine("\t}");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine($"\tvar {field} = TimeOnly.FromDateTime(DateTime.Now);");
                                template.AppendLine($"\tif (!string.IsNullOrEmpty(Request.Form[\"{field}\"]))");
                                template.AppendLine("\t{");
                                template.AppendLine(
                                    $"\tvar timeString = Request.Form[\"{field}\"].ToString().Split(\":\");");
                                template.AppendLine(
                                    $"\t{field} = new(Convert.ToInt32(timeString[0].ToString()), Convert.ToInt32(timeString[1].ToString()));");
                                template.AppendLine("\t}");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine(
                                    $"\tvar {field} =  !string.IsNullOrEmpty(Request.Form[\"{field}\"])?Convert.ToInt32(Request.Form[\"{field}\"]):0;");
                            }
                            else
                            {
                                template.AppendLine(
                                    $"\tvar {field} =  !string.IsNullOrEmpty(Request.Form[\"{field}\"])?Convert.ToInt32(Request.Form[\"{field}\"]):0;");
                            }
                        }
                        else if (GetDoubleDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine(
                                $"\tvar {field} =  !string.IsNullOrEmpty(Request.Form[\"{field}\"])?Convert.ToDouble(Request.Form[\"{field}\"]):0;");
                        }
                        else if (type.Contains("decimal"))
                        {
                            template.AppendLine(
                                $"\tvar {field} =  !string.IsNullOrEmpty(Request.Form[\"{field}\"])?Convert.ToDecimal(Request.Form[\"{field}\"]):0;");
                        }
                        else if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            // a bit long just for checking
                            template.AppendLine($"var {field} = Array.Empty<byte>();");
                            template.AppendLine("foreach (var formFile in Request.Form.Files)");
                            template.AppendLine("{");
                            template.AppendLine($"if (formFile.Name.Equals(\"{field}\"))");
                            template.AppendLine("{");
                            template.AppendLine(
                                $"    {field} = await sharedUtil.GetByteArrayFromImageAsync(formFile);");
                            template.AppendLine("}");
                            template.AppendLine("}");
                        }
                        else
                        {
                            template.AppendLine($"\tvar {field} = Request.Form[\"{field}\"];");
                        }
                    }
                }


                // end loop 
                template.AppendLine("            var search = Request.Form[\"search\"];");


                template.AppendLine($"           List<{ucTableName}Model> data = new();");
                template.AppendLine($"           {ucTableName}Model dataSingle = new();");
                template.AppendLine("            string code;");
                template.AppendLine("            var lastInsertKey=0;");
                template.AppendLine("            switch (mode)");
                template.AppendLine("            {");
                //create
                template.AppendLine("                case \"create\":");
                template.AppendLine(
                    "if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.CREATE_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
                // start loop
                template.AppendLine("                            {");
                template.Append(createModelString);
                template.AppendLine("                            };");
                // end loop
                template.AppendLine(
                    $"                           lastInsertKey = {lcTableName}Repository.Create({lcTableName}Model);");
                template.AppendLine(
                    "                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine(
                    "                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                //read
                template.AppendLine("                case \"read\":");
                template.AppendLine(
                    "                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"                           data = {lcTableName}Repository.Read();");

                template.AppendLine(
                    "                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine(
                    "                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                // search
                template.AppendLine("                case \"search\":");
                template.AppendLine(
                    "                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"       data = {lcTableName}Repository.Search(search);");
                template.AppendLine(
                    "                            code = ((int)ReturnCodeEnum.READ_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine(
                    "                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");

                // single record 
                template.AppendLine("                case \"single\":");
                template.AppendLine(
                    "                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
                // start loop
                template.AppendLine("                            {");
                template.AppendLine($"                                {ucTableName}Key = {lcTableName}Key");
                template.AppendLine("                            };");
                template.AppendLine(
                    $"                           dataSingle = {lcTableName}Repository.GetSingle({lcTableName}Model);");
                if (imageUpload)
                {
                    foreach (var field in imageFileName)
                    {
                        template.AppendLine("dataSingle." + field +
                                            "Base64String =sharedUtil.GetImageString(dataSingle." + field + ");");
                        template.AppendLine("dataSingle." + field + "= Array.Empty<byte>();");
                    }
                }

                template.AppendLine(
                    "                            code = ((int)ReturnCodeEnum.READ_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine(
                    "                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                // future might single with detail
                if (!string.IsNullOrEmpty(tableNameDetail))
                {
                    template.AppendLine("                case \"singleWithDetail\":");
                    template.AppendLine(
                        "                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
                    template.AppendLine("                    {");
                    template.AppendLine(
                        "                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                    template.AppendLine("                    }");
                    template.AppendLine("                    else");
                    template.AppendLine("                    {");
                    template.AppendLine("                        try");
                    template.AppendLine("                        {");
                    template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
                    // start loop
                    template.AppendLine("                            {");
                    template.AppendLine($"                                {ucTableName}Key = {lcTableName}Key");
                    template.AppendLine("                            };");
                    template.AppendLine(
                        $"                           dataSingle = {lcTableName}Repository.GetSingleWithDetail({lcTableName}Model);");
                    if (imageUpload)
                    {
                        foreach (var field in imageFileName)
                        {
                            template.AppendLine("dataSingle." + field +
                                                "Base64String =sharedUtil.GetImageString(dataSingle." + field + ");");
                            template.AppendLine("dataSingle." + field + "= new byte[0];");
                        }
                    }

                    template.AppendLine(
                        "                            code = ((int)ReturnCodeEnum.READ_SUCCESS).ToString();");
                    template.AppendLine("                            status = true;");
                    template.AppendLine("                        }");
                    template.AppendLine("                        catch (Exception ex)");
                    template.AppendLine("                        {");
                    template.AppendLine(
                        "                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                    template.AppendLine("                        }");
                    template.AppendLine("                    }");
                    template.AppendLine("                    break;");
                }

                // update
                template.AppendLine("                case \"update\":");
                template.AppendLine(
                    "                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.UPDATE_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
                // start loop
                template.AppendLine("                            {");
                template.Append(updateModelString);
                template.AppendLine("                            };");
                // end loop
                template.AppendLine($"                            {lcTableName}Repository.Update({lcTableName}Model);");
                template.AppendLine(
                    "                            code = ((int)ReturnCodeEnum.UPDATE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine(
                    "                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");

                // delete
                template.AppendLine("                case \"delete\":");
                template.AppendLine(
                    "                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.DELETE_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"                            {ucTableName}Model {lcTableName}Model = new()");
                // start loop
                template.AppendLine("                            {");
                template.AppendLine($"                                {ucTableName}Key = {lcTableName}Key");
                template.AppendLine("                            };");
                // end loop
                template.AppendLine($"                            {lcTableName}Repository.Delete({lcTableName}Model);");
                template.AppendLine(
                    "                            code = ((int)ReturnCodeEnum.DELETE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine(
                    "                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                template.AppendLine("                default:");
                template.AppendLine(
                    "                    code = ((int)ReturnCodeEnum.ACCESS_DENIED_NO_MODE).ToString();");
                template.AppendLine("                    break;");
                template.AppendLine("            }");
                template.AppendLine("            if (data.Count > 0)");
                template.AppendLine("            {");
                template.AppendLine("                return Ok(new { status, code, data });");
                template.AppendLine("            }");
                template.AppendLine("            if (mode.Equals(\"single\") || mode.Equals(\"singleWithDetail\"))");
                template.AppendLine("            {");
                template.AppendLine("                return Ok(new { status, code, dataSingle });");
                template.AppendLine("            }");
                template.AppendLine(
                    "            return lastInsertKey > 0 ? Ok(new { status, code, lastInsertKey }) : Ok(new { status, code });");
                template.AppendLine("        }");
                template.AppendLine("     ");
                template.AppendLine("    }");

                return template.ToString();
            }

            public string GeneratePages(string module, string tableName)
            {
                var primaryKey = GetPrimaryKeyTableName(tableName);
                var ucTableName = GetStringNoUnderScore(tableName, (int) TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int) TextCase.LcWords);
                var describeTableModels = GetTableStructure(tableName);
                //var fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();
                StringBuilder template = new();


                template.AppendLine("    try");
                template.AppendLine("    {");

                template.AppendLine("@inject IHttpContextAccessor _httpContextAccessor");
                template.AppendLine($"@using RebelCmsTemplate.Models.{module}");
                template.AppendLine("@using RebelCmsTemplate.Models.Shared");
                template.AppendLine($"@using RebelCmsTemplate.Repository.{module}");
                template.AppendLine("@using RebelCmsTemplate.Util;");
                template.AppendLine("@using RebelCmsTemplate.Enum;");
                template.AppendLine("@{");
                template.AppendLine("    SharedUtil sharedUtils = new(_httpContextAccessor);");
                template.AppendLine($"    List<{ucTableName}Model> {lcTableName}Models = new();");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (key.Equals("MUL"))
                    {
                        template.AppendLine(
                            $"    List<{UpperCaseFirst(field.Replace("Id", ""))}Model> {LowerCaseFirst(field.Replace("Id", ""))}Models = new();");
                    }
                }


                template.AppendLine(
                    $"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"       {lcTableName}Models = {lcTableName}Repository.Read();");

                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x)) || !key.Equals("MUL")) continue;
                    template.AppendLine(
                        $"       {UpperCaseFirst(field.Replace("Id", ""))}Repository {LowerCaseFirst(field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                    template.AppendLine(
                        $"       {LowerCaseFirst(field.Replace("Id", ""))}Models = {LowerCaseFirst(field.Replace("Id", ""))}Repository.Read();");
                }


                template.AppendLine("    }");
                template.AppendLine("    catch (Exception ex)");
                template.AppendLine("    {");
                template.AppendLine("        sharedUtils.SetSystemException(ex);");
                template.AppendLine("    }");
                template.AppendLine("    var fileInfo = ViewContext.ExecutingFilePath?.Split(\"/\");");
                template.AppendLine("    var filename = fileInfo != null ? fileInfo[4] : \"\";");
                template.AppendLine("    var name = filename.Split(\".\")[0];");
                template.AppendLine("    var navigationModel = sharedUtils.GetNavigation(name);");
                template.AppendLine("}");

                template.AppendLine("    <div class=\"page-title\">");
                template.AppendLine("        <div class=\"row\">");
                template.AppendLine("            <div class=\"col-12 col-md-6 order-md-1 order-last\">");
                template.AppendLine("                <h3>@navigationModel.LeafName</h3>");
                template.AppendLine("            </div>");
                template.AppendLine("            <div class=\"col-12 col-md-6 order-md-2 order-first\">");
                template.AppendLine(
                    "                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
                template.AppendLine("                    <ol class=\"breadcrumb\">");
                template.AppendLine("                        <li class=\"breadcrumb-item\">");
                template.AppendLine("                            <a href=\"#\">");
                template.AppendLine(
                    "                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
                template.AppendLine("                            </a>");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine(
                    "                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-file-excel\"></i>");
                template.AppendLine("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-sign-out-alt\"></i>");
                template.AppendLine("                            <a href=\"/logout\">Logout</a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                    </ol>");
                template.AppendLine("                </nav>");
                template.AppendLine("            </div>");
                template.AppendLine("        </div>");
                template.AppendLine("    </div>");
                template.AppendLine("    <section class=\"content\">");
                template.AppendLine("        <div class=\"container-fluid\">");
                template.AppendLine("            <form class=\"form-horizontal\">");
                template.AppendLine("                <div class=\"card card-primary\">");
                template.AppendLine("                    <div class=\"card-header\">Filter</div>");
                template.AppendLine("                    <div class=\"card-body\">");
                template.AppendLine("                        <div class=\"form-group\">");
                template.AppendLine("                            <div class=\"col-md-2\">");
                template.AppendLine("                                <label for=\"search\">Search</label>");
                template.AppendLine("                            </div>");
                template.AppendLine("                            <div class=\"col-md-10\">");
                template.AppendLine(
                    "                                <input name=\"search\" id=\"search\" class=\"form-control\"");
                template.AppendLine(
                    "                                    placeholder=\"Please Enter Name  Or Other Here\" maxlength=\"64\"");
                template.AppendLine("                                  style =\"width: 350px!important;\" />");
                template.AppendLine("                            </div>");
                template.AppendLine("                        </div>");
                template.AppendLine("                    </div>");
                template.AppendLine("                   <div class=\"card-footer\">");
                template.AppendLine(
                    "                        <button type=\"button\" class=\"btn btn-info\" onclick=\"searchRecord()\">");
                template.AppendLine("                            <i class=\"fas fa-filter\"></i> Filter");
                template.AppendLine("                        </button>");
                template.AppendLine("                        &nbsp;");
                template.AppendLine(
                    "                        <button type=\"button\" class=\"btn btn-warning\" onclick=\"resetRecord()\">");
                template.AppendLine("                            <i class=\"fas fa-power-off\"></i> Reset");
                template.AppendLine("                        </button>");
                template.AppendLine("                    </div>");
                template.AppendLine("                </div>");
                template.AppendLine("                <div class=\"row\">");
                template.AppendLine("                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp;</div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </form>");
                template.AppendLine("            <div class=\"row\">");
                template.AppendLine("                <div class=\"col-xs-12 col-sm-12 col-md-12\">");
                template.AppendLine("                    <div class=\"card\">");
                // start table
                template.AppendLine(
                    "                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
                template.AppendLine("                            <thead>");
                template.AppendLine("                                <tr>");
                // loop here
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing here 
                                break;
                            case "MUL":
                            {
                                template.AppendLine("<td>");
                                template.AppendLine(" <label>");
                                template.AppendLine(
                                    $" <select name=\"{field.Replace("Id", "Key")}\" id=\"{field.Replace("Id", "Key")}\" class=\"form-control\">");
                                template.AppendLine($"  @if ({field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("   {");
                                template.AppendLine("    <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("   }");
                                template.AppendLine("   else");
                                template.AppendLine("   {");
                                template.AppendLine("   foreach (var row" + UpperCaseFirst(field.Replace("Id", "")) +
                                                    " in " + LowerCaseFirst(field.Replace("Id", "")) + "Models)");
                                template.AppendLine("   {");
                                template.AppendLine("    <option value=\"@row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "Key\">");
                                var optionLabel = UpperCaseFirst(GetLabelOrPlaceHolderForComboBox(tableName, field));
                                template.AppendLine("     @row" + UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    optionLabel + "</option>");
                                template.AppendLine("   }");
                                template.AppendLine("  }");

                                template.AppendLine("  </select>");
                                template.AppendLine(" </label>");
                                template.AppendLine("</td>");
                                break;
                            }
                            default:
                            {
                                if (GetNumberDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine(" <td>");
                                    template.AppendLine("  <label>");
                                    template.AppendLine(
                                        $"   <input type=\"number\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                    template.AppendLine("  </label>");
                                    template.AppendLine(" </td>");
                                }
                                else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine(" <td>");
                                    template.AppendLine("  <label>");
                                    template.AppendLine(
                                        $"  <input type=\"number\" step=\"0.01\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                    template.AppendLine("  </label>");
                                    template.AppendLine(" </td>");
                                }
                                else if (GetDateDataType().Any(x => type.Contains(x)))
                                {
                                    if (type.Contains("datetime"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"datetime-local\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("date"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"date\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("time"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"time\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("year"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"number\" type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"text\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                }
                                else
                                {
                                    template.AppendLine("                                    <td>");
                                    template.AppendLine("                                        <label>");
                                    template.AppendLine(
                                        $"                                            <input type=\"text\" name=\"{field}\" id=\"{field}\" class=\"form-control\" />");
                                    template.AppendLine("                                        </label>");
                                    template.AppendLine("                                    </td>");
                                }

                                break;
                            }
                        }
                    }
                }

                // end loop
                template.AppendLine("                                    <td style=\"text-align: center\">");
                template.AppendLine(
                    "                                        <Button type=\"button\" class=\"btn btn-info\" onclick=\"createRecord()\">");
                template.AppendLine(
                    "                                            <i class=\"fa fa-newspaper\"></i>&nbsp;&nbsp;CREATE");
                template.AppendLine("                                        </Button>");
                template.AppendLine("                                    </td>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                                <tr>");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (!key.Equals("PRI"))
                    {
                        template.AppendLine("                                    <th>" +
                                            field.Replace(lcTableName, "").Replace("Id", "") + "</th>");
                    }
                }

                template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                            </thead>");
                template.AppendLine("                            <tbody id=\"tableBody\">");
                template.AppendLine($"                                @foreach (var row in {lcTableName}Models)");
                template.AppendLine("                                {");
                template.AppendLine(
                    $"                                    <tr id='{lcTableName}-@row.{ucTableName}Key'>");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing here 
                                break;
                            case "MUL":
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine(
                                    $"                                            <select name=\"{field.Replace("Id", "Key")}\" id=\"{field.Replace("Id", "Key")}-@row.{ucTableName}Key\" class=\"form-control\">");
                                template.AppendLine(
                                    $"                                              @if ({field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("                                                {");
                                template.AppendLine(
                                    "                                                  <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                                else");
                                template.AppendLine("                                                {");
                                template.AppendLine("                       foreach (var option in from row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + " in " +
                                                    LowerCaseFirst(field.Replace("Id", "")) + "Models");
                                template.AppendLine("                        let selected = row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "Key ==");
                                template.AppendLine("                       row." +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "Key");
                                template.AppendLine(
                                    "                       select selected ? Html.Raw(\"<option value='\" +");
                                template.AppendLine("                       row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    UpperCaseFirst(field.Replace("Id", "")) +
                                                    "Key + \"' selected>\" +");

                                var optionLabel = "row" + UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                  UpperCaseFirst(
                                                      GetLabelForComboBoxForGridOrOption(tableName, field));

                                template.AppendLine("                       " + optionLabel +
                                                    " + \"</option>\") :");
                                template.AppendLine("                       Html.Raw(\"<option value=\\\"\" + row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    UpperCaseFirst(field.Replace("Id", "Key")) + " +");

                                template.AppendLine("                       \"\\\">\" + " + optionLabel +
                                                    " + \"</option>\"))");
                                template.AppendLine(" {");
                                template.AppendLine("     @option");
                                template.AppendLine("                                  }");
                                template.AppendLine("                                               }");

                                template.AppendLine("                                             </select>");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");


                                break;
                            }
                            default:
                            {
                                if (GetNumberDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("                                    <td>");
                                    template.AppendLine("                                        <label>");
                                    template.AppendLine(
                                        $"                                            <input type=\"number\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\"  value=\"@row.{UpperCaseFirst(field)}\" class=\"form-control\" />");
                                    template.AppendLine("                                        </label>");
                                    template.AppendLine("                                    </td>");
                                }
                                else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("                                    <td>");
                                    template.AppendLine("                                        <label>");
                                    template.AppendLine(
                                        $"                                            <input type=\"number\" step=\"0.01\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\"  value=\"@row.{UpperCaseFirst(field)}\" class=\"form-control\" />");
                                    template.AppendLine("                                        </label>");
                                    template.AppendLine("                                    </td>");
                                }
                                else if (GetDateDataType().Any(x => type.Contains(x)))
                                {
                                    if (type.Contains("datetime"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"datetime-local\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(field)}.ToString(\"yyyy-MM-ddTHH:mm:ss\")\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("date"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"date\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(field)}.ToString(\"yyyy-MM-dd\")\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("time"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"time\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(field)}.ToString(\"HH:mm:ss\")\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("year"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(field)}.ToString(\"yyyy\")\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"text\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(field)}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                }
                                else
                                {
                                    template.AppendLine("                                    <td>");
                                    template.AppendLine("                                        <label>");
                                    template.AppendLine(
                                        $"                                            <input type=\"text\" name=\"{field}\" id=\"{field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(field)}\" class=\"form-control\" />");
                                    template.AppendLine("                                        </label>");
                                    template.AppendLine("                                    </td>");
                                }

                                break;
                            }
                        }
                    }
                }

                // loop here
                template.AppendLine("                                        <td style=\"text-align: center\">");
                template.AppendLine("                                            <div class=\"btn-group\">");
                template.AppendLine(
                    $"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord(@row.{ucTableName}Key)\">");
                template.AppendLine(
                    "                                                    <i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                                &nbsp;");
                template.AppendLine(
                    $"                                                <Button type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord(@row.{ucTableName}Key)\">");
                template.AppendLine(
                    "                                                    <i class=\"fas fa-trash\"></i>&nbsp;DELETE");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                            </div>");
                template.AppendLine("                                       </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine($"                                @if ({lcTableName}Models.Count == 0)");
                template.AppendLine("                                {");
                template.AppendLine("                                    <tr>");
                template.AppendLine("                                        <td colspan=\"7\" class=\"noRecord\">");
                template.AppendLine("                                           @SharedUtil.NoRecord");
                template.AppendLine("                                        </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine("                            </tbody>");
                template.AppendLine("                        </table>");
                // end table
                template.AppendLine("                    </div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </div>");
                template.AppendLine("        </div>");
                template.AppendLine("    </section>");
                template.AppendLine("    <script>");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    // later custom validator 
                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (key.Equals("MUL"))
                    {
                        template.AppendLine(" var " + field.Replace("Id", "") + "Models = @Json.Serialize(" +
                                            field.Replace("Id", "") + "Models);");
                    }
                }

                StringBuilder templateField = new();
                StringBuilder oneLineTemplateField = new();
                StringBuilder createTemplateField = new();
                //    StringBuilder updateTemplateField = new();

                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (key.Equals("PRI") || key.Equals("MUL"))
                    {
                        templateField.Append("row." + field.Replace("Id", "Key") + ",");
                        oneLineTemplateField.Append(field.Replace("Id", "Key") + ",");
                    }
                    else
                    {
                        templateField.Append("row." + field + ",");
                        oneLineTemplateField.Append(field + ",");
                        createTemplateField.Append(field + ".val(),");
                    }
                }


                // reset record
                template.AppendLine("        function resetRecord() {");
                template.AppendLine("         readRecord();");
                template.AppendLine("         $(\"#search\").val(\"\");");
                template.AppendLine("        }");

                // empty template
                template.AppendLine("        function emptyTemplate() {");
                template.AppendLine("         return\"<tr><td colspan='4'>It's lonely here</td></tr>\";");
                template.AppendLine("        }");

                // remember to one row template here as function name 
                template.AppendLine("        function template(" + oneLineTemplateField.ToString().TrimEnd(',') +
                                    ") {");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x)) || !key.Equals("MUL")) continue;
                    template.AppendLine("\tlet " + field.Replace("Id", "Key") + "Options = \"\";");
                    template.AppendLine("\tlet i = 0;");
                    template.AppendLine("\t" + field.Replace("Id", "") + "Models.map((row) => {");
                    template.AppendLine("\t\ti++;");
                    template.AppendLine("\t\tconst selected = (parseInt(row." + field.Replace("Id", "Key") +
                                        ") === parseInt(" + field.Replace("Id", "Key") +
                                        ")) ? \"selected\" : \"\";");

                    var optionLabel = UpperCaseFirst(GetLabelForComboBoxForGridOrOption(tableName, field));
                    template.AppendLine("\t\t" + field.Replace("Id", "Key") +
                                        "Options += \"<option value='\" + row." +
                                        field.Replace("Id", "Key") +
                                        " + \"' \" + selected + \">\" + row." + optionLabel +
                                        " +\"</option>\";");
                    template.AppendLine("\t});");
                }

                template.AppendLine("            let template =  \"\" +");
                template.AppendLine($"                \"<tr id='{lcTableName}-\" + {lcTableName}Key + \"'>\" +");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    continue;
                                case "MUL":
                                    template.AppendLine("\t\t\"<td class='tdNormalAlign'>\" +");
                                    template.AppendLine("\t\t\t\" <label>\" +");
                                    template.AppendLine("\t\t\t\t\"<select id='" + field.Replace("Id", "Key") +
                                                        "-\"+" + lcTableName + "Key+\"' class='form-control'>\";");
                                    template.AppendLine(
                                        "\t\ttemplate += " + field.Replace("Id", "Key") + "Options;");
                                    template.AppendLine("\t\ttemplate += \"</select>\" +");
                                    template.AppendLine("\t\t\"</label>\" +");
                                    template.AppendLine("\t\t\"</td>\" +");
                                    break;
                                default:
                                    template.AppendLine("\"<td>\" +");
                                    template.AppendLine(" \"<label>\" +");
                                    template.AppendLine("   \"<input type='number' name='" +
                                                        field.Replace("Id", "Key") + "' id='" +
                                                        field.Replace("Id", "Key") + "-\"+" + lcTableName +
                                                        "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                        "+\"' class='form-control' />\" +");
                                    template.AppendLine(" \"</label>\" +");
                                    template.AppendLine("\"</td>\" +");
                                    break;
                            }
                        }
                        else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\"<td>\" +");
                            template.AppendLine(" \"<label>\" +");
                            template.AppendLine("   \"<input type='number' step='0.01' name='" + field + "' id='" +
                                                field + "-\"+" + lcTableName + "Key+\"' value='\"+" +
                                                LowerCaseFirst(field) + "+\"' class='form-control' />\" +");
                            template.AppendLine(" \"</label>\" +");
                            template.AppendLine("\"</td>\" +");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='datetime-local' name='" + LowerCaseFirst(field) +
                                                    "' id='" + field.Replace("Id", "Key") + "-\"+" + lcTableName +
                                                    "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    "+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='date' name='" + LowerCaseFirst(field) +
                                                    "'  id='" + field.Replace("Id", "Key") + "-\"+" + lcTableName +
                                                    "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='time' name='" + LowerCaseFirst(field) +
                                                    "' id='" + field.Replace("Id", "Key") + "-\"+" + lcTableName +
                                                    "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='number' min='1900' max='2099' step='1' name='" +
                                                    LowerCaseFirst(field) + "' id='" + field.Replace("Id", "Key") +
                                                    "-\"+" + lcTableName + "Key+\"' value='\"+" +
                                                    LowerCaseFirst(field) +
                                                    ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='text' name='" + LowerCaseFirst(field) +
                                                    "' id='" + field.Replace("Id", "Key") + "-\"+" + lcTableName +
                                                    "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    "+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("                                   \"</td>\" +");
                            }
                        }
                        else
                        {
                            template.AppendLine("\"<td>\" +");
                            template.AppendLine(" \"<label>\" +");
                            template.AppendLine("   \"<input type='text' name='" + LowerCaseFirst(field) + "' id='" +
                                                field.Replace("Id", "Key") + "-\"+" + lcTableName +
                                                "Key+\"'' value='\"+" + LowerCaseFirst(field) +
                                                "+\"' class='form-control' />\" +");
                            template.AppendLine(" \"</label>\" +");
                            template.AppendLine("\"</td>\" +");
                        }
                    }
                }

                template.AppendLine("                \"<td style='text-align: center'><div class='btn-group'>\" +");
                template.AppendLine(
                    $"                \"<Button type='button' class='btn btn-warning' onclick='updateRecord(\" + {lcTableName}Key + \")'>\" +"
                );
                template.AppendLine("                \"<i class='fas fa-edit'></i> UPDATE\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"&nbsp;\" +");
                template.AppendLine(
                    $"                \"<Button type='button' class='btn btn-danger' onclick='deleteRecord(\" + {lcTableName}Key + \")'>\" +"
                );
                template.AppendLine("                \"<i class='fas fa-trash'></i> DELETE\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"</div></td>\" +");
                template.AppendLine("                \"</tr>\";");
                template.AppendLine("               return template; ");
                template.AppendLine("        }");

                // create record
                template.AppendLine("        function createRecord() {");
                // loop here 
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                continue;
                            case "MUL":
                                template.AppendLine(
                                    $" const {field.Replace("Id", "Key")} = $(\"#{field.Replace("Id", "Key")}\");");
                                break;
                            default:
                                template.AppendLine($" const {field} = $(\"#{field}\");");
                                break;
                        }
                    }
                }

                // loop here
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: 'POST',");
                template.AppendLine("           url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("           async: false,");
                template.AppendLine("           data: {");
                template.AppendLine("            mode: 'create',");
                template.AppendLine("            leafCheckKey: @navigationModel.LeafCheckKey,");
                // loop here
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                continue;
                            case "MUL":
                                template.AppendLine(
                                    $"            {field.Replace("Id", "Key")}: {field.Replace("Id", "Key")}.val(),");
                                break;
                            default:
                                template.AppendLine($"            {field}: {field}.val(),");
                                break;
                        }
                    }
                }

                // loop here
                template.AppendLine("           },statusCode: {");
                template.AppendLine("            500: function () {");
                template.AppendLine(
                    "             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("           }");
                template.AppendLine("          },");
                template.AppendLine("          beforeSend: function () {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("            if (data === void 0) {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("            let status = data.status;");
                template.AppendLine("            let code = data.code;");
                template.AppendLine("            if (status) {");
                template.AppendLine("             const lastInsertKey = data.lastInsertKey;");
                template.AppendLine("             $(\"#tableBody\").prepend(template(lastInsertKey," +
                                    createTemplateField.ToString().TrimEnd(',') + "));");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("               title: 'Success!',");
                template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                template.AppendLine("               icon: 'success',");
                template.AppendLine("               confirmButtonText: 'Cool'");
                template.AppendLine("             });");
                // loop here
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    switch (key)
                    {
                        case "PRI":
                            continue;
                        case "MUL":
                            template.AppendLine($"\t{field.Replace("Id", "Key")}.val('');");

                            break;
                        default:
                            template.AppendLine("\t" + field + ".val('');");

                            break;
                    }
                }

                // loop here
                template.AppendLine("            } else if (status === false) {");
                template.AppendLine("             if (typeof(code) === 'string'){");
                template.AppendLine("             @{");
                template.AppendLine(
                    "              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");

                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }else{");
                template.AppendLine("               <text>");
                template.AppendLine(
                    "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine(
                    "            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");

                template.AppendLine("             let timerInterval=0;");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine(
                    "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");

                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("                Swal.showLoading();");
                template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                timerInterval = setInterval(() => {");
                template.AppendLine("                b.textContent = Swal.getTimerLeft();");
                template.AppendLine("               }, 100);");
                template.AppendLine("              },");
                template.AppendLine("              willClose: () => {");
                template.AppendLine("               clearInterval(timerInterval);");
                template.AppendLine("              }");
                template.AppendLine("            }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("            });");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          } else {");
                template.AppendLine("           location.href = \"/\";");
                template.AppendLine("          }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("          console.log(xhr.status);");
                template.AppendLine(
                    "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                template.AppendLine("        function readRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"read\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) {");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                template.AppendLine("               if (data.data === void 0) {");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("               } else {");
                template.AppendLine("                if (data.data.length > 0) {");
                template.AppendLine("                 let templateStringBuilder = \"\";");
                template.AppendLine("                 for (let i = 0; i < data.data.length; i++) {");
                template.AppendLine("                  let row = data.data[i];");

                // remember one line row 
                template.AppendLine("                  templateStringBuilder += template(" +
                                    templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                } else {");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");

                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");

                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");

                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("              }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("              });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");
                template.AppendLine("        function searchRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"search\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("           search: $(\"#search\").val()");
                template.AppendLine("          }, ");
                template.AppendLine("          statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("            if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("             let status =data.status;");
                template.AppendLine("             let code = data.code;");
                template.AppendLine("             if (status) {");
                template.AppendLine("              if (data.data === void 0) {");
                template.AppendLine("               $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("              } else {");
                template.AppendLine("               if (data.data.length > 0) {");
                template.AppendLine("                let templateStringBuilder = \"\";");
                template.AppendLine("                for (let i = 0; i < data.data.length; i++) {");
                template.AppendLine("                 let row = data.data[i];");
                // remember one line row 

                template.AppendLine("                 templateStringBuilder += template(" +
                                    templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                }");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("               }");
                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");

                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");

                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");

                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval); }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("          console.log(xhr.status);");
                template.AppendLine(
                    "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableName + "\");");
                template.AppendLine("        }");
                template.AppendLine("        function updateRecord(" + lcTableName + "Key) {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: 'POST',");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: 'update',");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");

                // loop here
                if (primaryKey != null)
                {
                    template.AppendLine("           " + primaryKey.Replace("Id", "Key") + ": " +
                                        primaryKey.Replace("Id", "Key") + ",");
                }

                // loop not primary
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (key.Equals("PRI") || key.Equals("MUL"))
                    {
                        if (field == primaryKey) continue;
                        if (primaryKey != null)
                        {
                            template.AppendLine(
                                $"           {field.Replace("Id", "Key")}: $(\"#{field.Replace("Id", "Key")}-\" + {primaryKey.Replace("Id", "Key")}).val(),");
                        }
                    }
                    else
                    {
                        if (primaryKey != null)
                        {
                            template.AppendLine(
                                $"           {field}: $(\"#{field}-\" + {primaryKey.Replace("Id", "Key")}).val(),");
                        }
                    }
                }

                // loop here
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("           }");
                template.AppendLine("          },");
                template.AppendLine("          beforeSend: function () {");
                template.AppendLine("           console.log(\"loading..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("           if (data === void 0) {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("           let status = data.status;");
                template.AppendLine("           let code = data.code;");
                template.AppendLine("           if (status) {");
                template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success');");
                template.AppendLine("           } else if (status === false) {");
                template.AppendLine("            if (typeof(code) === 'string'){");
                template.AppendLine("            @{");
                template.AppendLine(
                    "             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");

                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("              else");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine(
                    "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine(
                    "            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");

                template.AppendLine("             let timerInterval=0;");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine(
                    "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");

                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("              Swal.showLoading();");
                template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("               timerInterval = setInterval(() => {");
                template.AppendLine("               b.textContent = Swal.getTimerLeft();");
                template.AppendLine("              }, 100);");
                template.AppendLine("             },");
                template.AppendLine("             willClose: () => {");
                template.AppendLine("              clearInterval(timerInterval);");
                template.AppendLine("             }");
                template.AppendLine("            }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");
                template.AppendLine("        function deleteRecord(" + lcTableName + "Key) { ");
                template.AppendLine("         Swal.fire({");
                template.AppendLine("          title: 'Are you sure?',");
                template.AppendLine("          text: \"You won't be able to revert this!\",");
                template.AppendLine("          icon: 'warning',");
                template.AppendLine("          showCancelButton: true,");
                template.AppendLine("          confirmButtonText: 'Yes, delete it!',");
                template.AppendLine("          cancelButtonText: 'No, cancel!',");
                template.AppendLine("          reverseButtons: true");
                template.AppendLine("         }).then((result) => {");
                template.AppendLine("          if (result.value) {");
                template.AppendLine("           $.ajax({");
                template.AppendLine("            type: 'POST',");
                template.AppendLine("            url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("            async: false,");
                template.AppendLine("            data: {");
                template.AppendLine("             mode: 'delete',");
                template.AppendLine("             leafCheckKey: @navigationModel.LeafCheckKey,");
                if (primaryKey != null)
                {
                    template.AppendLine("             " + primaryKey.Replace("Id", "Key") + ": " +
                                        primaryKey.Replace("Id", "Key") + " ");
                }

                template.AppendLine("            }, statusCode: {");
                template.AppendLine("             500: function () {");
                template.AppendLine(
                    "              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("             }");
                template.AppendLine("            },");
                template.AppendLine("            beforeSend: function () {");
                template.AppendLine("             console.log(\"loading..\");");
                template.AppendLine("           }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                if (primaryKey != null)
                {
                    template.AppendLine("               $(\"#" + lcTableName + "-\" + " +
                                        primaryKey.Replace("Id", "Key") +
                                        ").remove();");
                }

                template.AppendLine(
                    "               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");

                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");

                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");

                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");

                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");

                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("       } else if (result.dismiss === swal.DismissReason.cancel) {");
                template.AppendLine("        Swal.fire({");
                template.AppendLine("          icon: 'error',");
                template.AppendLine("          title: 'Cancelled',");
                template.AppendLine("          text: 'Be careful before delete record'");
                template.AppendLine("        })");
                template.AppendLine("       }");
                template.AppendLine("      });");
                template.AppendLine("    }");
                template.AppendLine("    </script>");


                return template.ToString();
            }

            public string GeneratePagesFormAndGrid(string module, string tableName)
            {
                var primaryKey = GetPrimaryKeyTableName(tableName);

                const int gridMax = 6;
                StringBuilder template = new();

                var ucTableName = GetStringNoUnderScore(tableName, (int) TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int) TextCase.LcWords);
                var describeTableModels = GetTableStructure(tableName);

                var fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();

                template.AppendLine("@inject IHttpContextAccessor _httpContextAccessor");
                template.AppendLine($"@using RebelCmsTemplate.Models.{module}");
                template.AppendLine("@using RebelCmsTemplate.Models.Shared");
                template.AppendLine($"@using RebelCmsTemplate.Repository.{module}");
                template.AppendLine("@using RebelCmsTemplate.Util;");
                template.AppendLine("@using RebelCmsTemplate.Enum;");
                template.AppendLine("@{");
                template.AppendLine("    SharedUtil sharedUtils = new(_httpContextAccessor);");
                template.AppendLine($"    List<{ucTableName}Model> {lcTableName}Models = new();");

                var imageUpload = false;
                List<string> imageFileName = new();

                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        if (key.Equals("MUL"))
                        {
                            template.AppendLine(
                                $"    List<{UpperCaseFirst(field.Replace("Id", ""))}Model> {LowerCaseFirst(field.Replace("Id", ""))}Models = new();");
                        }
                    }

                    if (GetBlobType().Any(x => type.Contains(x)))
                    {
                        imageFileName.Add(UpperCaseFirst(field));
                    }
                }

                if (imageFileName.Count > 0)
                {
                    imageUpload = true;
                }

                template.AppendLine("    try");
                template.AppendLine("    {");


                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x)) || !key.Equals("MUL")) continue;
                    template.AppendLine(
                        $"       {UpperCaseFirst(field.Replace("Id", ""))}Repository {LowerCaseFirst(field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                    template.AppendLine(
                        $"       {LowerCaseFirst(field.Replace("Id", ""))}Models = {LowerCaseFirst(field.Replace("Id", ""))}Repository.Read();");
                }

                // prevent session problem
                template.AppendLine(
                    $"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"       {lcTableName}Models = {lcTableName}Repository.Read();");


                template.AppendLine("    }");
                template.AppendLine("    catch (Exception ex)");
                template.AppendLine("    {");
                template.AppendLine("        sharedUtils.SetSystemException(ex);");
                template.AppendLine("    }");
                template.AppendLine("    var fileInfo = ViewContext.ExecutingFilePath?.Split(\"/\");");
                template.AppendLine("    var filename = fileInfo != null ? fileInfo[4] : \"\";");
                template.AppendLine("    var name = filename.Split(\".\")[0];");
                template.AppendLine("    NavigationModel navigationModel = sharedUtils.GetNavigation(name);");
                template.AppendLine("}");

                template.AppendLine("    <div class=\"page-title\">");
                template.AppendLine("        <div class=\"row\">");
                template.AppendLine("            <div class=\"col-12 col-md-6 order-md-1 order-last\">");
                template.AppendLine("                <h3>@navigationModel.LeafName</h3>");
                template.AppendLine("            </div>");
                template.AppendLine("            <div class=\"col-12 col-md-6 order-md-2 order-first\">");
                template.AppendLine(
                    "                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
                template.AppendLine("                    <ol class=\"breadcrumb\">");
                template.AppendLine("                        <li class=\"breadcrumb-item\">");
                template.AppendLine("                            <a href=\"#\">");
                template.AppendLine(
                    "                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
                template.AppendLine("                            </a>");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine(
                    "                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-file-excel\"></i>");
                template.AppendLine("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-sign-out-alt\"></i>");
                template.AppendLine("                            <a href=\"/logout\">Logout</a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                    </ol>");
                template.AppendLine("                </nav>");
                template.AppendLine("            </div>");
                template.AppendLine("        </div>");
                template.AppendLine("    </div>");
                template.AppendLine("    <section class=\"content\">");
                template.AppendLine("        <div class=\"container-fluid\">");
                template.AppendLine("            <form class=\"form-horizontal\">");
                template.AppendLine("                <div class=\"card card-primary\">");
                template.AppendLine("                    <div class=\"card-header\">");
                // this is button create / update / delete
                // only premium edition like 10 years ago get a lot  more
                // default update and delete disabled
                template.AppendLine(
                    "                                                <Button id=\"createButton\" type=\"button\" class=\"btn btn-success\" onclick=\"createRecord()\">");
                template.AppendLine(
                    "                                                    <i class=\"fas fa-newspaper\"></i>&nbsp;CREATE");
                template.AppendLine("                                                </Button>&nbsp;");
                template.AppendLine(
                    "                                                <Button id=\"updateButton\" type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord()\" disabled=\"disabled\">");
                template.AppendLine(
                    "                                                    <i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
                template.AppendLine("                                                </Button>&nbsp;");

                template.AppendLine(
                    "                                                <Button id=\"deleteButton\" type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord()\" disabled=\"disabled\">");
                template.AppendLine(
                    "                                                    <i class=\"fas fa-trash\"></i>&nbsp;DELETE");

                template.AppendLine("                                                </Button>&nbsp;");

                template.AppendLine(
                    "                        <button type=\"button\" class=\"btn btn-warning\" onclick=\"resetForm()\">");
                template.AppendLine("                            <i class=\"fas fa-power-off\"></i> Reset");
                template.AppendLine("                        </button>");


                template.AppendLine("                    </div>");
                template.AppendLine("                    <div class=\"card-body\">");
                template.AppendLine("         <div class=\"row\">");
                var d = 0;
                var i = 0;
                var total = describeTableModels.Count;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    // some part from db prefer to limit the value soo we just push it if was string of number 
                    var maxLength = Regex.Replace(type, @"[^0-9]+", "");
                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing here
                                template.AppendLine(
                                    $"\t<input type=\"hidden\" id=\"{field.Replace("Id", "Key")}\" value=\"0\" />");
                                // don't calculate this for two
                                d--;
                                break;
                            case "MUL":


                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("\t<div class=\"form-group\">");
                                template.AppendLine(
                                    $"\t\t<label for=\"{field.Replace("Id", "Key")}\">{SplitToSpaceLabel(field.Replace("Id", ""))}</label>");
                                template.AppendLine(
                                    $"                                            <select name=\"{field.Replace("Id", "Key")}\" id=\"{LowerCaseFirst(field.Replace("Id", "Key"))}\" class=\"form-control\">");
                                template.AppendLine(
                                    $"                                              @if ({field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("                                                {");
                                template.AppendLine(
                                    "                                                  <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                                else");
                                template.AppendLine("                                                {");
                                template.AppendLine(
                                    "                                                foreach (var row" +
                                    UpperCaseFirst(field.Replace("Id", "")) + " in " +
                                    LowerCaseFirst(field.Replace("Id", "")) + "Models)");
                                template.AppendLine("                                                {");
                                template.AppendLine(
                                    "                                                   <option value=\"@row" +
                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                    UpperCaseFirst(field.Replace("Id", "Key")) + "\">");

                                template.AppendLine("                                                   @row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    UpperCaseFirst(
                                                        GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                    "</option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                               }");

                                template.AppendLine("                                             </select>");

                                template.AppendLine("\t</div>");
                                template.AppendLine("\t</div>");


                                break;

                            default:
                            {
                                if (GetNumberDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"number\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\" placeholder=\"\"  value=\"0\" maxlength=\"" +
                                        maxLength + "\" />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }
                                else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"number\" step=\"0.01\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"0\" maxlength=\"" +
                                        maxLength + "\"  />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }
                                else if (GetDateDataType().Any(x => type.Contains(x)))
                                {
                                    if (type.Contains("datetime"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"datetime-local\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\" />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else if (type.Contains("date"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"date\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\" />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else if (type.Contains("time"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"time\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\"  />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else if (type.Contains("year"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\"  />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"text\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\" \"  maxlength=\"" +
                                            maxLength + "\" />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                }
                                else if (GetBlobType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"file\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\" onchange=\"showPreview(event,'{LowerCaseFirst(field)}Image');\" />");
                                    // if you want multi image need to alter the db and create new mime field
                                    template.AppendLine(
                                        $"\t<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+P+/HgAFhAJ/wlseKgAAAABJRU5ErkJggg==\" id=\"{LowerCaseFirst(field)}Image\" class=\"img-fluid\"  accept=\"image/png\" style=\"width:100px;height:100px\" />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }
                                else
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"text\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\" \" maxlength=\"" +
                                        maxLength + "\"  />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }

                                break;
                            }
                        }
                    }

                    if (d == 2)
                    {
                        template.AppendLine("        </div>");
                        if (i != total)
                        {
                            template.AppendLine("         <div class=\"row\">");
                        }

                        d = 0;
                    }

                    i++;
                    d++;
                }
                // loop here
                // end form here 


                template.AppendLine("                    </div>");

                template.AppendLine("                </div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </form>");


                template.AppendLine("                <div class=\"row\">");
                template.AppendLine("                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp;</div>");
                template.AppendLine("                </div>");


                template.AppendLine("<div class=\"card\">");
                template.AppendLine("\t<div class=\"card-header\">");
                template.AppendLine("\t\t<label for=\"search\">Search</label>");
                template.AppendLine("\t</div>");
                template.AppendLine("\t<div class=\"card-body\">");
                template.AppendLine(
                    "\t\t<input name=\"search\" id=\"search\" class=\"form-control\" placeholder=\"Please Enter Name  Or Other Here\" maxlength=\"64\" style =\"width: 350px!important;\" />");
                template.AppendLine("\t</div>");

                template.AppendLine("\t<div class=\"card-footer\">");
                template.AppendLine("\t\t<button type=\"button\" class=\"btn btn-info\" onclick=\"searchRecord()\">");
                template.AppendLine("\t\t\t<i class=\"fas fa-filter\"></i> Filter");
                template.AppendLine("\t\t</button>");
                template.AppendLine("\t\t&nbsp;");
                template.AppendLine("\t\t<button type=\"button\" class=\"btn btn-warning\" onclick=\"resetRecord()\">");
                template.AppendLine("\t\t\t<i class=\"fas fa-power-off\"></i> Reset");
                template.AppendLine("\t\t</button>");
                template.AppendLine("\t</div>");
                template.AppendLine("</div>");

                template.AppendLine("                <div class=\"row\">");
                template.AppendLine("                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp;</div>");
                template.AppendLine("                </div>");


                // grid still existed but on more edit entry max first 6 info only and read only 

                template.AppendLine("            <div class=\"row\">");
                template.AppendLine("                <div class=\"col-xs-12 col-sm-12 col-md-12\">");
                template.AppendLine("                    <div class=\"card\">");
                template.AppendLine(
                    "                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
                template.AppendLine("                            <thead>");
                template.AppendLine("                            <tr>");
                var h = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (!key.Equals("PRI"))
                    {
                        template.AppendLine("                                    <th>" +
                                            SplitToSpaceLabel(field.Replace(lcTableName, "").Replace("Id", "")) +
                                            "</th>");
                        i++;
                    }

                    if (h == gridMax)
                    {
                        break;
                    }

                    h++;
                }

                template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                            </thead>");
                template.AppendLine("                            <tbody id=\"tableBody\">");
                template.AppendLine($"                                @foreach (var row in {lcTableName}Models)");
                template.AppendLine("                                {");
                template.AppendLine(
                    $"                                    <tr id='{lcTableName}-@row.{ucTableName}Key'>");
                var j = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing here 
                                break;
                            case "MUL":


                                template.AppendLine("<td>");
                                template.AppendLine("@row." +
                                                    UpperCaseFirst(
                                                        GetLabelForComboBoxForGridOrOption(tableName, field)));
                                template.AppendLine("</td>");


                                break;

                            default:
                            {
                                if (GetNumberDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine(
                                        $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                }
                                else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine(
                                        $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                }
                                else if (GetDateDataType().Any(x => type.Contains(x)))
                                {
                                    if (type.Contains("datetime"))
                                    {
                                        template.AppendLine(
                                            $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else if (type.Contains("date"))
                                    {
                                        template.AppendLine(
                                            $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else if (type.Contains("time"))
                                    {
                                        template.AppendLine(
                                            $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else if (type.Contains("year"))
                                    {
                                        template.AppendLine(
                                            $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else
                                    {
                                        template.AppendLine(
                                            $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                }
                                else
                                {
                                    template.AppendLine(
                                        $"                                    <td>@row.{UpperCaseFirst(field)}</td>");
                                }

                                break;
                            }
                        }

                        if (j == gridMax)
                        {
                            break;
                        }

                        j++;
                    }
                }

                // loop here
                template.AppendLine("                                        <td style=\"text-align: center\">");
                template.AppendLine(
                    $"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"viewRecord(@row.{ucTableName}Key)\">");
                template.AppendLine(
                    "                                                    <i class=\"fas fa-edit\"></i>&nbsp;VIEW");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                       </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine($"                                @if ({lcTableName}Models.Count == 0)");
                template.AppendLine("                                {");
                template.AppendLine("                                    <tr>");
                template.AppendLine("                                        <td colspan=\"" +
                                    describeTableModels.Count + "\" class=\"noRecord\">");
                template.AppendLine("                                           @SharedUtil.NoRecord");
                template.AppendLine("                                        </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine("                            </tbody>");
                template.AppendLine("                        </table>");
                template.AppendLine("                    </div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </div>");
                template.AppendLine("        </div>");
                template.AppendLine("    </section>");
                template.AppendLine("    <script>");
                // hmm seem missing here . validator  later

                StringBuilder templateField = new();
                StringBuilder oneLineTemplateField = new();
                StringBuilder createTemplateField = new();
                //  StringBuilder updateTemplateField = new();
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    // later custom validator 
                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (key.Equals("MUL"))
                    {
                        template.AppendLine(" var " + field.Replace("Id", "") + "Models = @Json.Serialize(" +
                                            field.Replace("Id", "") + "Models);");
                    }
                }

                var uu = 0;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    switch (key)
                    {
                        case "PRI":
                        case "MUL":
                            templateField.Append("row." + field.Replace("Id", "Key") + ",");
                            if (uu < 6)
                            {
                                oneLineTemplateField.Append(field.Replace("Id", "Key") + ",");
                            }

                            uu++;
                            break;
                        default:
                            templateField.Append("row." + field + ",");
                            if (uu < 6)
                            {
                                oneLineTemplateField.Append(field + ",");
                            }

                            createTemplateField.Append(field + ".val(),");
                            uu++;
                            break;
                    }
                }


                template.AppendLine("\tfunction resetForm() {");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                    {
                        name = fieldName;
                    }

                    if (name.Contains("Id"))
                    {
                        template.AppendLine("\t$(\"#" + name.Replace("Id", "Key") + "\").val('');");
                    }
                    else
                    {
                        template.AppendLine("\t$(\"#" + name + "\").val('');");
                    }
                }

                template.AppendLine("            $(\"#createButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("        }");
                template.AppendLine("        function resetRecord() {");
                template.AppendLine("         readRecord();");
                template.AppendLine("         $(\"#search\").val(\"\");");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                    {
                        name = fieldName;
                    }

                    if (name.Contains("Id"))
                    {
                        template.AppendLine("\t$(\"#" + name.Replace("Id", "Key") + "\").val('');");
                    }
                    else
                    {
                        template.AppendLine("\t$(\"#" + name + "\").val('');");
                    }
                }

                template.AppendLine("        }");
                // empty template
                template.AppendLine("        function emptyTemplate() {");
                template.AppendLine("         return\"<tr><td colspan='" + gridMax + "'>It's lonely here</td></tr>\";");
                template.AppendLine("        }");

                template.AppendLine("        function emptyDetailTemplate() {");
                template.AppendLine("         return\"<tr><td colspan='" + describeTableModels.Count +
                                    "'>It's lonely here</td></tr>\";");
                template.AppendLine("        }");

                // remember to one row template here as function name
                // this only for view purpose only 
                template.AppendLine("        function template(" + oneLineTemplateField.ToString().TrimEnd(',') +
                                    ") {");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x)) || !key.Equals("MUL")) continue;
                    template.AppendLine("\tlet " + field.Replace("Id", "Key") + "Options = \"\";");
                    template.AppendLine("\t" + field.Replace("Id", "") + "Models.map((row) => {");
                    template.AppendLine("\t\tconst selected = (parseInt(row." + field.Replace("Id", "Key") +
                                        ") === parseInt(" + field.Replace("Id", "Key") +
                                        ")) ? \"selected\" : \"\";");
                    var optionLabel = GetLabelForComboBoxForGridOrOption(tableName, field);
                    template.AppendLine("\t\t" + field.Replace("Id", "Key") +
                                        "Options += \"<option value='\" + row." + field.Replace("Id", "Key") +
                                        " + \"' \" + selected + \">\" + row." + UpperCaseFirst(optionLabel) +
                                        " +\"</option>\";");
                    template.AppendLine("\t});");
                }

                template.AppendLine("            let template =  \"\" +");
                template.AppendLine($"                \"<tr id='{lcTableName}-\" + {lcTableName}Key + \"'>\" +");
                var m = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    continue;
                                case "MUL":
                                    template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");

                                    break;
                                default:
                                    template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");

                                    break;
                            }
                        }
                        else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                        }
                        else
                        {
                            template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                        }

                        if (m == gridMax)
                        {
                            break;
                        }

                        m++;
                    }
                }

                template.AppendLine("                \"<td style='text-align: center'><div class='btn-group'>\" +");
                template.AppendLine(
                    $"                \"<Button type='button' class='btn btn-warning' onclick='viewRecord(\" + {lcTableName}Key + \")'>\" +");
                template.AppendLine("                \"<i class='fas fa-search'></i> View\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"</div></td>\" +");
                template.AppendLine("                \"</tr>\";");
                template.AppendLine("               return template; ");
                template.AppendLine("        }");
                // if there is upload will be using form-data instead 
                if (imageUpload)
                {
                    template.AppendLine("        function createRecord() {");

                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    continue;

                                case "MUL":

                                    template.AppendLine(
                                        $" const {field.Replace("Id", "Key")} = $(\"#{field.Replace("Id", "Key")}\");");
                                    break;
                                default:
                                    template.AppendLine($" const {field} = $(\"#{field}\");");

                                    break;
                            }
                        }
                    }

                    template.AppendLine("          var formData = new FormData();");
                    template.AppendLine(" formData.append(\"mode\",\"create\");");
                    template.AppendLine(" formData.append(\"leafCheckKey\",@navigationModel.LeafCheckKey);");

                    // loop here 
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    template.AppendLine(
                                        $"        formData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");
                                    break;
                                case "MUL":

                                    template.AppendLine(
                                        $"        formData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");


                                    break;

                                default:
                                {
                                    if (GetNumberDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetDateDataType().Any(x => type.Contains(x)))
                                    {
                                        if (type.Contains("datetime"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("date"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("time"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("year"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                    }
                                    else if (GetBlobType().Any(x => type.Contains(x)))
                                    {
                                        // we check the size more then something ..
                                        template.AppendLine(
                                            $"var files{UpperCaseFirst(field)} = $('#{LowerCaseFirst(field)}')[0].files;");
                                        template.AppendLine("if(files" + UpperCaseFirst(field) + ".length > 0 ){");
                                        template.AppendLine("        formData.append('" + LowerCaseFirst(field) +
                                                            "',files" +
                                                            UpperCaseFirst(field) + "[0]);");
                                        template.AppendLine("}");
                                    }
                                    else
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    // loop here
                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("           url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("           async: false,");
                    template.AppendLine("           contentType: false,");
                    template.AppendLine("           processData: false, ");
                    template.AppendLine("           data: formData,");
                    template.AppendLine("           statusCode: {");
                    template.AppendLine("            500: function () {");
                    template.AppendLine(
                        "             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          },");
                    template.AppendLine("          beforeSend: function () {");
                    template.AppendLine("           console.log(\"loading ..\");");
                    template.AppendLine("          }}).done(function(data)  {");
                    template.AppendLine("            if (data === void 0) {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("            let status = data.status;");
                    template.AppendLine("            let code = data.code;");
                    template.AppendLine("            if (status) {");
                    template.AppendLine("             const lastInsertKey = data.lastInsertKey;");
                    template.AppendLine("             $(\"#tableBody\").prepend(template(lastInsertKey," +
                                        createTemplateField.ToString().TrimEnd(',') + "));");
                    // put value in input hidden
                    // flip create button disabled
                    // flip update button enabled
                    // flip disabled button enabled
                    if (primaryKey != null)
                    {
                        template.AppendLine("  $(\"#" + LowerCaseFirst(primaryKey.Replace("Id", "Key")) +
                                            "\").val(lastInsertKey);");
                    }

                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("               title: 'Success!',");
                    template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                    template.AppendLine("               icon: 'success',");
                    template.AppendLine("               confirmButtonText: 'Cool'");
                    template.AppendLine("             });");

                    template.AppendLine("            } else if (status === false) {");
                    template.AppendLine("             if (typeof(code) === 'string'){");
                    template.AppendLine("             @{");
                    template.AppendLine(
                        "              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }else{");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("                Swal.showLoading();");
                    template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("                timerInterval = setInterval(() => {");
                    template.AppendLine("                b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("               }, 100);");
                    template.AppendLine("              },");
                    template.AppendLine("              willClose: () => {");
                    template.AppendLine("               clearInterval(timerInterval);");
                    template.AppendLine("              }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("            });");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          } else {");
                    template.AppendLine("           location.href = \"/\";");
                    template.AppendLine("          }");
                    template.AppendLine("         }).fail(function(xhr)  {");
                    template.AppendLine("          console.log(xhr.status);");
                    template.AppendLine(
                        "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("         }).always(function (){");
                    template.AppendLine("          console.log(\"always:complete\");    ");
                    template.AppendLine("         });");
                    template.AppendLine("        }");
                }
                else
                {
                    template.AppendLine("        function createRecord() {");
                    // loop here 
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    continue;

                                case "MUL":

                                    template.AppendLine(
                                        $" const {field.Replace("Id", "Key")} = $(\"#{field.Replace("Id", "Key")}\");");
                                    break;
                                default:
                                    template.AppendLine($" const {field} = $(\"#{field}\");");

                                    break;
                            }
                        }
                    }

                    // loop here
                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("           url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("           async: false,");
                    template.AppendLine("           data: {");
                    template.AppendLine("            mode: 'create',");
                    template.AppendLine("            leafCheckKey: @navigationModel.LeafCheckKey,");
                    // loop here
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    continue;

                                case "MUL":
                                    template.AppendLine($"            {field.Replace("Id", "Key")}: $(\"#" +
                                                        field.Replace("Id", "Key") + "\").val(),");
                                    break;
                                default:
                                    template.AppendLine($"            {field}: $(\"#" + field + "\").val(),");

                                    break;
                            }
                        }
                    }

                    // loop here
                    template.AppendLine("           },statusCode: {");
                    template.AppendLine("            500: function () {");
                    template.AppendLine(
                        "             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          },");
                    template.AppendLine("          beforeSend: function () {");
                    template.AppendLine("           console.log(\"loading ..\");");
                    template.AppendLine("          }}).done(function(data)  {");
                    template.AppendLine("            if (data === void 0) {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("            let status = data.status;");
                    template.AppendLine("            let code = data.code;");
                    template.AppendLine("            if (status) {");
                    template.AppendLine("             const lastInsertKey = data.lastInsertKey;");
                    template.AppendLine("             $(\"#tableBody\").prepend(template(lastInsertKey," +
                                        createTemplateField.ToString().TrimEnd(',') + "));");
                    // put value in input hidden
                    // flip create button disabled
                    // flip update button enabled
                    // flip disabled button enabled
                    if (primaryKey != null)
                    {
                        template.AppendLine("  $(\"#" + LowerCaseFirst(primaryKey.Replace("Id", "Key")) +
                                            "\").val(lastInsertKey);");
                    }

                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("               title: 'Success!',");
                    template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                    template.AppendLine("               icon: 'success',");
                    template.AppendLine("               confirmButtonText: 'Cool'");
                    template.AppendLine("             });");

                    template.AppendLine("            } else if (status === false) {");
                    template.AppendLine("             if (typeof(code) === 'string'){");
                    template.AppendLine("             @{");
                    template.AppendLine(
                        "              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }else{");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("                Swal.showLoading();");
                    template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("                timerInterval = setInterval(() => {");
                    template.AppendLine("                b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("               }, 100);");
                    template.AppendLine("              },");
                    template.AppendLine("              willClose: () => {");
                    template.AppendLine("               clearInterval(timerInterval);");
                    template.AppendLine("              }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("            });");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          } else {");
                    template.AppendLine("           location.href = \"/\";");
                    template.AppendLine("          }");
                    template.AppendLine("         }).fail(function(xhr)  {");
                    template.AppendLine("          console.log(xhr.status);");
                    template.AppendLine(
                        "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("         }).always(function (){");
                    template.AppendLine("          console.log(\"always:complete\");    ");
                    template.AppendLine("         });");
                    template.AppendLine("        }");
                }

                // read record
                template.AppendLine("        function readRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"read\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) {");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                template.AppendLine("               if (data.data === void 0) {");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("               } else {");
                template.AppendLine("                if (data.data.length > 0) {");
                template.AppendLine("                 let templateStringBuilder = \"\";");
                template.AppendLine("                 for (let i = 0; i < data.data.length; i++) {");
                template.AppendLine("                  let row = data.data[i];");
                // remember one line row 
                template.AppendLine("                  templateStringBuilder += template(" +
                                    templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                } else {");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("              }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("              });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");
                // search record
                template.AppendLine("        function searchRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"search\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("           search: $(\"#search\").val()");
                template.AppendLine("          }, ");
                template.AppendLine("          statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("            if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("             let status =data.status;");
                template.AppendLine("             let code = data.code;");
                template.AppendLine("             if (status) {");
                template.AppendLine("              if (data.data === void 0) {");
                template.AppendLine("               $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("              } else {");
                template.AppendLine("               if (data.data.length > 0) {");
                template.AppendLine("                let templateStringBuilder = \"\";");
                template.AppendLine("                for (let i = 0; i < data.data.length; i++) {");
                template.AppendLine("                 let row = data.data[i];");
                // remember one line row 

                template.AppendLine("                 templateStringBuilder += template(" +
                                    templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                }");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("               }");
                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval); }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("          console.log(xhr.status);");
                template.AppendLine(
                    "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                // excel record
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableName + "\");");
                template.AppendLine("        }");
                // view record
                if (primaryKey != null)
                {
                    template.AppendLine("        function viewRecord(" +
                                        LowerCaseFirst(primaryKey.Replace("Id", "Key")) + ") {");
                }

                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"singleWithDetail\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                if (primaryKey != null)
                {
                    template.AppendLine("           " + LowerCaseFirst(primaryKey.Replace("Id", "Key")) + ": " +
                                        LowerCaseFirst(primaryKey.Replace("Id", "Key")));
                }

                template.AppendLine("          }, ");
                template.AppendLine("          statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("            if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("             let status =data.status;");
                template.AppendLine("             let code = data.code;");
                template.AppendLine("             if (status) {");
                template.AppendLine("              if (data.dataSingle === void 0) {");
                template.AppendLine("               $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("              } else {");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                case "MUL":
                                    template.AppendLine("\t$(\"#" + LowerCaseFirst(field.Replace("Id", "Key")) +
                                                        "\").val(data.dataSingle." +
                                                        LowerCaseFirst(field.Replace("Id", "Key")) + ");");
                                    break;
                                default:
                                    template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                        LowerCaseFirst(field) + ");");
                                    break;
                            }
                        }
                        else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                LowerCaseFirst(field) + ");");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                        }
                        else if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\t$(\"#" + LowerCaseFirst(field) +
                                                "Image\").attr(\"src\",data.dataSingle." + LowerCaseFirst(field) +
                                                "Base64String);");
                        }
                        else
                        {
                            template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                LowerCaseFirst(field) + ");");
                        }
                    }
                }

                template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"html, body\").animate({ scrollTop: 0 }, \"slow\");");

                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval); }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("          console.log(xhr.status);");
                template.AppendLine(
                    "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableName + "\");");
                template.AppendLine("        }");
                // update record
                if (imageUpload)
                {
                    template.AppendLine("        function updateRecord() {");
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }


                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                case "MUL":
                                    template.AppendLine(
                                        " const " + field.Replace("Id", "Key") + " = $(\"#" +
                                        field.Replace("Id", "Key") +
                                        "\");");
                                    break;
                                default:
                                    template.AppendLine(" const " + field + "} = $(\"#" + field + "\");");

                                    break;
                            }
                        }
                    }

                    template.AppendLine(" var formData = new FormData();");
                    template.AppendLine(" formData.append(\"mode\",\"update\");");
                    template.AppendLine(" formData.append(\"leafCheckKey\",@navigationModel.LeafCheckKey);");

                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    template.AppendLine(
                                        $"        formData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");
                                    break;
                                case "MUL":


                                    template.AppendLine(
                                        $"        formData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");


                                    break;

                                default:
                                {
                                    if (GetNumberDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetDateDataType().Any(x => type.Contains(x)))
                                    {
                                        if (type.Contains("datetime"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("date"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("time"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("year"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                    }
                                    else if (GetBlobType().Any(x => type.Contains(x)))
                                    {
                                        // we check the size more then something ..
                                        template.AppendLine(
                                            $"var files{UpperCaseFirst(field)} = $('#{LowerCaseFirst(field)}')[0].files;");
                                        template.AppendLine("if(files" + UpperCaseFirst(field) + ".length > 0 ){");
                                        template.AppendLine("        formData.append('" + LowerCaseFirst(field) +
                                                            "',files" +
                                                            UpperCaseFirst(field) + "[0]);");
                                        template.AppendLine("}");
                                    }
                                    else
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("           async: false,");
                    template.AppendLine("           contentType: false,");
                    template.AppendLine("           processData: false, ");
                    template.AppendLine("           data: formData,");
                    template.AppendLine("          statusCode: {");
                    template.AppendLine("           500: function () {");
                    template.AppendLine(
                        "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          }, beforeSend() {");
                    template.AppendLine("           console.log(\"loading..\");");
                    template.AppendLine("          }}).done(function(data)  {");
                    template.AppendLine("           if (data === void 0) {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("           let status = data.status;");
                    template.AppendLine("           let code = data.code;");
                    template.AppendLine("           if (status) {");
                    // flip the update button enabled
                    // flip the delete button enable
                    // flip the create button disabled
                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success');");
                    template.AppendLine("           } else if (status === false) {");
                    template.AppendLine("            if (typeof(code) === 'string'){");
                    template.AppendLine("            @{");
                    template.AppendLine(
                        "             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("              else");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("              Swal.showLoading();");
                    template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("               timerInterval = setInterval(() => {");
                    template.AppendLine("               b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("              }, 100);");
                    template.AppendLine("             },");
                    template.AppendLine("             willClose: () => {");
                    template.AppendLine("              clearInterval(timerInterval);");
                    template.AppendLine("             }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("             });");
                    template.AppendLine("            } else {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          }).fail(function(xhr)  {");
                    template.AppendLine("           console.log(xhr.status);");
                    template.AppendLine(
                        "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("          }).always(function (){");
                    template.AppendLine("           console.log(\"always:complete\");    ");
                    template.AppendLine("          });");
                    template.AppendLine("        }");
                }
                else
                {
                    template.AppendLine("        function updateRecord() {");
                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("          async: false,");
                    template.AppendLine("          data: {");
                    template.AppendLine("           mode: 'update',");
                    template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                    // loop here
                    template.AppendLine("             " + lcTableName + "Key: $(\"#" + lcTableName + "Key\").val(),");
                    // loop not primary
                    foreach (var fieldName in fieldNameList)
                    {
                        var name = string.Empty;
                        if (fieldName != null)
                        {
                            name = fieldName;
                        }

                        if (GetHiddenField().Any(x => name.Contains(x))) continue;
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"            {name.Replace("Id", "Key")}: $(\"#" +
                                                name.Replace("Id", "Key") + "\").val(),");
                        }
                        else
                        {
                            template.AppendLine($"            {name}: $(\"#" + name + "\").val(),");
                        }
                    }

                    // loop here
                    template.AppendLine("          }, statusCode: {");
                    template.AppendLine("           500: function () {");
                    template.AppendLine(
                        "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          },");
                    template.AppendLine("          beforeSend: function () {");
                    template.AppendLine("           console.log(\"loading..\");");
                    template.AppendLine("          }}).done(function(data)  {");
                    template.AppendLine("           if (data === void 0) {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("           let status = data.status;");
                    template.AppendLine("           let code = data.code;");
                    template.AppendLine("           if (status) {");
                    // flip the update button enabled
                    // flip the delete button enable
                    // flip the create button disabled
                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success');");
                    template.AppendLine("           } else if (status === false) {");
                    template.AppendLine("            if (typeof(code) === 'string'){");
                    template.AppendLine("            @{");
                    template.AppendLine(
                        "             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("              else");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("              Swal.showLoading();");
                    template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("               timerInterval = setInterval(() => {");
                    template.AppendLine("               b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("              }, 100);");
                    template.AppendLine("             },");
                    template.AppendLine("             willClose: () => {");
                    template.AppendLine("              clearInterval(timerInterval);");
                    template.AppendLine("             }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("             });");
                    template.AppendLine("            } else {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          }).fail(function(xhr)  {");
                    template.AppendLine("           console.log(xhr.status);");
                    template.AppendLine(
                        "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("          }).always(function (){");
                    template.AppendLine("           console.log(\"always:complete\");    ");
                    template.AppendLine("          });");
                    template.AppendLine("        }");
                }

                // delete record
                template.AppendLine("        function deleteRecord() { ");
                template.AppendLine("         Swal.fire({");
                template.AppendLine("          title: 'Are you sure?',");
                template.AppendLine("          text: \"You won't be able to revert this!\",");
                template.AppendLine("          type: 'warning',");
                template.AppendLine("          showCancelButton: true,");
                template.AppendLine("          confirmButtonText: 'Yes, delete it!',");
                template.AppendLine("          cancelButtonText: 'No, cancel!',");
                template.AppendLine("          reverseButtons: true");
                template.AppendLine("         }).then((result) => {");
                template.AppendLine("          if (result.value) {");
                template.AppendLine("           $.ajax({");
                template.AppendLine("            type: 'POST',");
                template.AppendLine("            url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("            async: false,");
                template.AppendLine("            data: {");
                template.AppendLine("             mode: 'delete',");
                template.AppendLine("             leafCheckKey: @navigationModel.LeafCheckKey,");
                if (primaryKey != null)
                {
                    template.AppendLine("             " + LowerCaseFirst(primaryKey.Replace("Id", "Key")) + ": $(\"#" +
                                        LowerCaseFirst(primaryKey.Replace("Id", "Key")) + "\").val()");
                }

                template.AppendLine("            }, statusCode: {");
                template.AppendLine("             500: function () {");
                template.AppendLine(
                    "              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("             }");
                template.AppendLine("            },");
                template.AppendLine("            beforeSend: function () {");
                template.AppendLine("             console.log(\"loading..\");");
                template.AppendLine("           }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                // reset hidden key primary key
                // flip the update button disabled
                // flip the delete button disabled
                // flip the create button enabled
                // reset all record 
                template.AppendLine("            $(\"#createButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").attr(\"disabled\",\"disabled\");");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                    {
                        name = fieldName;
                    }

                    if (name.Contains("Id"))
                    {
                        template.AppendLine("\t$(\"#" + name.Replace("Id", "Key") + "\").val('');");
                    }
                    else
                    {
                        template.AppendLine("\t$(\"#" + name + "\").val('');");
                    }
                }

                template.AppendLine("               readRecord();");
                template.AppendLine(
                    "               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("       } else if (result.dismiss === swal.DismissReason.cancel) {");
                template.AppendLine("        Swal.fire({");
                template.AppendLine("          icon: 'error',");
                template.AppendLine("          title: 'Cancelled',");
                template.AppendLine("          text: 'Be careful before delete record'");
                template.AppendLine("        })");
                template.AppendLine("       }");
                template.AppendLine("      });");
                template.AppendLine("    }");
                template.AppendLine("    </script>");


                return template.ToString();
            }

            public string GenerateMasterAndDetail(string module, string tableName, string tableNameDetail)
            {
                var primaryKey = GetPrimaryKeyTableName(tableName);
                var primaryKeyDetail = GetPrimaryKeyTableName(tableNameDetail);

                const int gridMax = 6;
                StringBuilder template = new();

                var ucTableName = GetStringNoUnderScore(tableName, (int) TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int) TextCase.LcWords);

                // var ucTableDetailName = GetStringNoUnderScore(tableNameDetail, (int)TextCase.UcWords);
                var lcTableDetailName = GetStringNoUnderScore(tableNameDetail, (int) TextCase.LcWords);

                var describeTableModels = GetTableStructure(tableName);
                var fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();

                var describeTableDetailModels = GetTableStructure(tableNameDetail);
                //var fieldNameDetailList = describeTableDetailModels.Select(x => x.FieldValue).ToList();

                template.AppendLine("@inject IHttpContextAccessor _httpContextAccessor");
                template.AppendLine($"@using RebelCmsTemplate.Models.{module}");
                template.AppendLine("@using RebelCmsTemplate.Models.Shared");
                template.AppendLine($"@using RebelCmsTemplate.Repository.{module}");
                template.AppendLine("@using RebelCmsTemplate.Util;");
                template.AppendLine("@using RebelCmsTemplate.Enum;");
                template.AppendLine("@{");
                template.AppendLine("    SharedUtil sharedUtils = new(_httpContextAccessor);");
                template.AppendLine($"    List<{ucTableName}Model> {lcTableName}Models = new();");

                var imageUpload = false;
                List<string> imageFileName = new();

                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (key.Equals("MUL"))
                        {
                            template.AppendLine(
                                $"    List<{UpperCaseFirst(field.Replace("Id", ""))}Model> {LowerCaseFirst(field.Replace("Id", ""))}Models = new();");
                        }

                        if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            imageFileName.Add(UpperCaseFirst(field));
                        }
                    }
                }


                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (key.Equals("MUL"))
                        {
                            if (!field.Equals(primaryKey))
                            {
                                template.AppendLine(
                                    $"    List<{UpperCaseFirst(field.Replace("Id", ""))}Model> {LowerCaseFirst(field.Replace("Id", ""))}Models = new();");
                            }
                        }

                        if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            imageFileName.Add(UpperCaseFirst(field));
                        }
                    }
                }

                if (imageFileName.Count > 0)
                {
                    imageUpload = true;
                }

                template.AppendLine("    try");
                template.AppendLine("    {");

                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;

                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!key.Equals("MUL")) continue;
                    template.AppendLine(
                        $"       {UpperCaseFirst(field.Replace("Id", ""))}Repository {LowerCaseFirst(field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                    template.AppendLine(
                        $"       {LowerCaseFirst(field.Replace("Id", ""))}Models = {LowerCaseFirst(field.Replace("Id", ""))}Repository.Read();");
                }

                template.AppendLine(
                    $"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"       {lcTableName}Models = {lcTableName}Repository.Read();");


                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x)) || !key.Equals("MUL") ||
                        field.Equals(primaryKey)) continue;
                    template.AppendLine(
                        $"       {UpperCaseFirst(field.Replace("Id", ""))}Repository {LowerCaseFirst(field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                    template.AppendLine(
                        $"       {LowerCaseFirst(field.Replace("Id", ""))}Models = {LowerCaseFirst(field.Replace("Id", ""))}Repository.Read();");
                }

                template.AppendLine("    }");
                template.AppendLine("    catch (Exception ex)");
                template.AppendLine("    {");
                template.AppendLine("        sharedUtils.SetSystemException(ex);");
                template.AppendLine("    }");
                template.AppendLine("    var fileInfo = ViewContext.ExecutingFilePath?.Split(\"/\");");
                template.AppendLine("    var filename = fileInfo != null ? fileInfo[4] : \"\";");
                template.AppendLine("    var name = filename.Split(\".\")[0];");
                template.AppendLine("    NavigationModel navigationModel = sharedUtils.GetNavigation(name);");
                template.AppendLine("}");

                template.AppendLine("    <div class=\"page-title\">");
                template.AppendLine("        <div class=\"row\">");
                template.AppendLine("            <div class=\"col-12 col-md-6 order-md-1 order-last\">");
                template.AppendLine("                <h3>@navigationModel.LeafName</h3>");
                template.AppendLine("            </div>");
                template.AppendLine("            <div class=\"col-12 col-md-6 order-md-2 order-first\">");
                template.AppendLine(
                    "                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
                template.AppendLine("                    <ol class=\"breadcrumb\">");
                template.AppendLine("                        <li class=\"breadcrumb-item\">");
                template.AppendLine("                            <a href=\"#\">");
                template.AppendLine(
                    "                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
                template.AppendLine("                            </a>");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine(
                    "                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-file-excel\"></i>");
                template.AppendLine("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
                template.AppendLine("                        </li>");
                template.AppendLine(
                    "                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-sign-out-alt\"></i>");
                template.AppendLine("                            <a href=\"/logout\">Logout</a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                    </ol>");
                template.AppendLine("                </nav>");
                template.AppendLine("            </div>");
                template.AppendLine("        </div>");
                template.AppendLine("    </div>");
                template.AppendLine("    <section class=\"content\">");
                template.AppendLine("        <div class=\"container-fluid\">");


                template.AppendLine("    <div id=\"listView\">");
                // search bar
                template.AppendLine("<div class=\"card\" id=\"searchBox\">");
                template.AppendLine("\t<div class=\"card-header\">");
                template.AppendLine("\t\t<label for=\"search\">Search</label>");
                template.AppendLine("\t</div>");
                template.AppendLine("\t<div class=\"card-body\">");
                template.AppendLine(
                    "\t\t<input name=\"search\" id=\"search\" class=\"form-control\" placeholder=\"Please Enter Name  Or Other Here\" maxlength=\"64\" style =\"width: 350px!important;\" />");
                template.AppendLine("\t</div>");

                template.AppendLine("\t<div class=\"card-footer\">");
                template.AppendLine("\t\t<button type=\"button\" class=\"btn btn-info\" onclick=\"searchRecord()\">");
                template.AppendLine("\t\t\t<i class=\"fas fa-filter\"></i> Filter");
                template.AppendLine("\t\t</button>");
                template.AppendLine("\t\t&nbsp;");
                template.AppendLine("\t\t<button type=\"button\" class=\"btn btn-warning\" onclick=\"resetRecord()\">");
                template.AppendLine("\t\t\t<i class=\"fas fa-power-off\"></i> Reset");
                template.AppendLine("\t\t</button>");
                template.AppendLine("\t\t&nbsp;");
                template.AppendLine("\t\t<button type=\"button\" class=\"btn btn-success\" onclick=\"newRecord()\">");
                template.AppendLine("\t\t\t<i class=\"fas fa-plus\"></i> Create New  Record");
                template.AppendLine("\t\t</button>");
                template.AppendLine("\t</div>");
                template.AppendLine("</div>");
                // end search bar

                // table search . this is static view 

                template.AppendLine("                <div class=\"row\">");
                template.AppendLine("                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp;</div>");
                template.AppendLine("                </div>");
                template.AppendLine("                    <div class=\"card\">");
                template.AppendLine(
                    "                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
                template.AppendLine("                            <thead>");
                template.AppendLine("                            <tr>");
                var y = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (!key.Equals("PRI"))
                    {
                        template.AppendLine("\t<th>" +
                                            SplitToSpaceLabel(field.Replace(lcTableName, "").Replace("Id", "")) +
                                            "</th>");
                    }

                    if (y == gridMax)
                    {
                        break;
                    }

                    y++;
                }

                template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                            </thead>");
                template.AppendLine("                            <tbody id=\"tableBody\">");
                template.AppendLine($"                                @foreach (var row in {lcTableName}Models)");
                template.AppendLine("                                {");
                template.AppendLine(
                    $"                                    <tr id='{lcTableName}-@row.{ucTableName}Key'>");
                var u = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing here 
                                break;
                            case "MUL":
                                template.AppendLine(
                                    $"<td>@row.{UpperCaseFirst(GetLabelForComboBoxForGridOrOption(tableName, field))}</td>");
                                break;
                            default:
                            {
                                if (GetNumberDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                }
                                else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                }
                                else if (GetDateDataType().Any(x => type.Contains(x)))
                                {
                                    if (type.Contains("datetime"))
                                    {
                                        template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else if (type.Contains("date"))
                                    {
                                        template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else if (type.Contains("time"))
                                    {
                                        template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else if (type.Contains("year"))
                                    {
                                        template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                    else
                                    {
                                        template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                    }
                                }
                                else
                                {
                                    template.AppendLine($"<td>@row.{UpperCaseFirst(field)}</td>");
                                }

                                break;
                            }
                        }

                        if (u == gridMax)
                        {
                            break;
                        }

                        u++;
                    }
                }

                // loop here
                template.AppendLine("                                        <td style=\"text-align: center\">");
                template.AppendLine(
                    $"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"viewRecord(@row.{ucTableName}Key)\">");
                template.AppendLine(
                    "                                                    <i class=\"fas fa-edit\"></i>&nbsp;VIEW");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                       </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine($"                                @if ({lcTableName}Models.Count == 0)");
                template.AppendLine("                                {");
                template.AppendLine("                                    <tr>");
                template.AppendLine("                                        <td colspan=\"" +
                                    describeTableModels.Count + "\" class=\"noRecord\">");
                template.AppendLine("                                           @SharedUtil.NoRecord");
                template.AppendLine("                                        </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine("                            </tbody>");
                template.AppendLine("                        </table>");
                template.AppendLine("    </div>");
                template.AppendLine("    </div>");

                // this section more on when clicked a the list will come here. hide above dom . still using old still

                template.AppendLine("   <div id=\"formView\" style=\"display:none\">");

                template.AppendLine("            <form class=\"form-horizontal\">");
                template.AppendLine("                <div class=\"card card-primary\">");
                template.AppendLine("                    <div class=\"card-header\">");
                // this is button create / update / delete
                // only premium edition like 10 years ago get a lot  more
                // default update and delete disabled
                template.AppendLine(
                    "\t<Button id=\"createButton\" type=\"button\" class=\"btn btn-success\" onclick=\"createRecord()\">");
                template.AppendLine("\t\t<i class=\"fas fa-newspaper\"></i>&nbsp;CREATE");
                template.AppendLine("\t</Button>&nbsp;");

                template.AppendLine(
                    "\t<Button id=\"updateButton\" type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord()\" disabled=\"disabled\">");
                template.AppendLine("\t\t<i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
                template.AppendLine("\t</Button>&nbsp;");

                template.AppendLine(
                    "\t<Button id=\"deleteButton\" type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord()\" disabled=\"disabled\">");
                template.AppendLine("\t\t<i class=\"fas fa-trash\"></i>&nbsp;DELETE");
                template.AppendLine("\t</Button>&nbsp;");

                template.AppendLine("\t<button type=\"button\" class=\"btn btn-warning\" onclick=\"resetForm()\">");
                template.AppendLine("\t\t<i class=\"fas fa-power-off\"></i>&nbsp;RESET");
                template.AppendLine("\t</button>");

                template.AppendLine(
                    "\t<Button id=\"viewListButton\" type=\"button\" class=\"btn btn-danger\" onclick=\"viewListRecord()\">");
                template.AppendLine("\t\t<i class=\"fas fa-list\"></i>&nbsp;LIST");
                template.AppendLine("\t</Button>&nbsp;");


                template.AppendLine("                    </div>");
                template.AppendLine("                    <div class=\"card-body\">");

                template.AppendLine("         <div class=\"row\">");
                var d = 0;
                var i = 0;
                var total = describeTableModels.Count;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    // some part from db prefer to limit the value soo we just push it if was string of number 
                    var maxLength = Regex.Replace(type, @"[^0-9]+", "");
                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing here
                                template.AppendLine(
                                    $"\t<input type=\"hidden\" id=\"{field.Replace("Id", "Key")}\" value=\"0\" />");
                                // don't calculate this for two
                                d--;
                                break;
                            case "MUL":
                            {
                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("\t<div class=\"form-group\">");
                                template.AppendLine(
                                    $"\t\t<label for=\"{field.Replace("Id", "Key")}\">{SplitToSpaceLabel(field.Replace("Id", ""))}</label>");
                                template.AppendLine(
                                    $"\t\t<select name=\"{field.Replace("Id", "Key")}\" id=\"{LowerCaseFirst(field.Replace("Id", "Key"))}\" class=\"form-control\">");
                                template.AppendLine($"\t\t@if ({field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("\t\t{");
                                template.AppendLine("\t\t\t<option value=\"\">Please Create A New field </option>");
                                template.AppendLine("\t\t}");
                                template.AppendLine("\t\telse");
                                template.AppendLine("\t\t{");
                                template.AppendLine("\t\tforeach (var row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) +
                                                    " in " + LowerCaseFirst(field.Replace("Id", "")) + "Models)");
                                template.AppendLine("\t\t{");
                                template.AppendLine("\t\t\t<option value=\"@row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    UpperCaseFirst(field.Replace("Id", "Key")) + "\"> @row" +
                                                    UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                    UpperCaseFirst(
                                                        GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                    " </option>");
                                template.AppendLine("\t\t}");
                                template.AppendLine("\t\t}");

                                template.AppendLine("</select>");

                                template.AppendLine("\t</div>");
                                template.AppendLine("\t</div>");


                                break;
                            }
                            default:
                            {
                                if (GetNumberDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"number\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\" placeholder=\"\"  value=\"0\" maxlength=\"" +
                                        maxLength + "\" />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }
                                else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"number\" step=\"0.01\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"0\" maxlength=\"" +
                                        maxLength + "\"  />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }
                                else if (GetDateDataType().Any(x => type.Contains(x)))
                                {
                                    if (type.Contains("datetime"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"datetime-local\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\" />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else if (type.Contains("date"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"date\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\" />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else if (type.Contains("time"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"time\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\"  />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else if (type.Contains("year"))
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\"\"  />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                    else
                                    {
                                        template.AppendLine("<div class=\"col-md-6\">");
                                        template.AppendLine("<div class=\"form-group\">");
                                        template.AppendLine(
                                            $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                        template.AppendLine(
                                            $"\t<input type=\"text\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\" \"  maxlength=\"" +
                                            maxLength + "\" />");
                                        template.AppendLine("</div>");
                                        template.AppendLine("</div>");
                                    }
                                }
                                else if (GetBlobType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"file\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\" onchange=\"showPreview(event,'{LowerCaseFirst(field)}Image');\" />");
                                    // if you want multi image need to alter the db and create new mime field
                                    template.AppendLine(
                                        $"\t<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+P+/HgAFhAJ/wlseKgAAAABJRU5ErkJggg==\" id=\"{LowerCaseFirst(field)}Image\" class=\"img-fluid\"  accept=\"image/png\" style=\"width:100px;height:100px\" />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }
                                else
                                {
                                    template.AppendLine("<div class=\"col-md-6\">");
                                    template.AppendLine("<div class=\"form-group\">");
                                    template.AppendLine(
                                        $"\t<label for=\"{LowerCaseFirst(field)}\">{SplitToSpaceLabel(field.Replace(tableName, ""))}</label>");
                                    template.AppendLine(
                                        $"\t<input type=\"text\" id=\"{LowerCaseFirst(field)}\" class=\"form-control\"  value=\" \" maxlength=\"" +
                                        maxLength + "\"  />");
                                    template.AppendLine("</div>");
                                    template.AppendLine("</div>");
                                }

                                break;
                            }
                        }
                    }

                    if (d == 2)
                    {
                        template.AppendLine("        </div>");
                        if (i != total)
                        {
                            template.AppendLine("         <div class=\"row\">");
                        }

                        d = 0;
                    }

                    i++;
                    d++;
                }
                // loop here
                // end form here 


                template.AppendLine("                    </div>");

                template.AppendLine("                </div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </form>");


                template.AppendLine("                <div class=\"row\">");
                template.AppendLine("                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp;</div>");
                template.AppendLine("                </div>");

                template.AppendLine("            <div class=\"row\">");
                template.AppendLine("                <div class=\"col-xs-12 col-sm-12 col-md-12\">");
                template.AppendLine("                    <div class=\"card\">");
                // start table detail 
                // here we don't limit to 6 . But to design must be simplify don't till 10 > over !
                template.AppendLine(
                    "                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
                template.AppendLine("                            <thead>");
                template.AppendLine("                                <tr>");
                // loop here
                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing here 
                                break;
                            case "MUL":
                            {
                                if (!field.Equals(primaryKey))
                                {
                                    template.AppendLine("<td>");
                                    template.AppendLine("\t<label>");
                                    template.AppendLine(
                                        $"\t\t<select name=\"{field.Replace("Id", "Key")}\" id=\"detail_{field.Replace("Id", "Key")}\" class=\"form-control\">");
                                    template.AppendLine($"\t\t@if ({field.Replace("Id", "")}Models.Count == 0)");
                                    template.AppendLine("\t\t{");
                                    template.AppendLine("\t\t\t<option value=\"\">Please Create A New field </option>");
                                    template.AppendLine("\t\t}");
                                    template.AppendLine("\t\telse");
                                    template.AppendLine("\t\t{");
                                    template.AppendLine("\t\tforeach (var row" +
                                                        UpperCaseFirst(field.Replace("Id", "")) + " in " +
                                                        LowerCaseFirst(field.Replace("Id", "")) + "Models)");
                                    template.AppendLine("\t\t{");
                                    template.AppendLine("\t\t\t<option value=\"@row" +
                                                        UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                        UpperCaseFirst(field.Replace("Id", "Key")) + "\">");
                                    template.AppendLine("\t\t@row" + UpperCaseFirst(field.Replace("Id", "")) + "." +
                                                        UpperCaseFirst(
                                                            GetLabelForComboBoxForGridOrOption(tableNameDetail,
                                                                field)) + "</option>");
                                    template.AppendLine("\t\t}");
                                    template.AppendLine("\t\t}");

                                    template.AppendLine("\t\t</select>");
                                    template.AppendLine("\t</label>");
                                    template.AppendLine("</td>");
                                }

                                break;
                            }
                            default:
                            {
                                if (GetNumberDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("                                    <td>");
                                    template.AppendLine("                                        <label>");
                                    template.AppendLine(
                                        $"                                            <input type=\"number\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                    template.AppendLine("                                        </label>");
                                    template.AppendLine("                                    </td>");
                                }
                                else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                {
                                    template.AppendLine("                                    <td>");
                                    template.AppendLine("                                        <label>");
                                    template.AppendLine(
                                        $"                                            <input type=\"number\" step=\"0.01\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                    template.AppendLine("                                        </label>");
                                    template.AppendLine("                                    </td>");
                                }
                                else if (GetDateDataType().Any(x => type.Contains(x)))
                                {
                                    if (type.Contains("datetime"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"datetime-local\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("date"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"date\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("time"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"time\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else if (type.Contains("year"))
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"number\" type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                    else
                                    {
                                        template.AppendLine("                                    <td>");
                                        template.AppendLine("                                        <label>");
                                        template.AppendLine(
                                            $"                                            <input type=\"text\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                        template.AppendLine("                                        </label>");
                                        template.AppendLine("                                    </td>");
                                    }
                                }
                                else
                                {
                                    template.AppendLine("                                    <td>");
                                    template.AppendLine("                                        <label>");
                                    template.AppendLine(
                                        $"                                            <input type=\"text\" name=\"{field}\" id=\"detail_{field}\" class=\"form-control\" />");
                                    template.AppendLine("                                        </label>");
                                    template.AppendLine("                                    </td>");
                                }

                                break;
                            }
                        }
                    }
                }


                template.AppendLine("\t\t<td style=\"text-align: center\">");
                template.AppendLine(
                    "\t\t\t<Button id=\"createDetailButton\" type=\"button\" class=\"btn btn-info\" onclick=\"createDetailRecord()\" disabled=\"disabled\">");
                template.AppendLine("\t\t\t\t<i class=\"fa fa-newspaper\"></i>&nbsp;&nbsp;CREATE");
                template.AppendLine("\t\t\t</Button>");
                template.AppendLine("\t\t</td>");
                template.AppendLine("\t</tr>");
                template.AppendLine("\t<tr>");
                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key.Equals("PRI"))
                        {
                            case false:
                            {
                                if (!field.Equals(primaryKey))
                                {
                                    template.AppendLine("\t<th>" +
                                                        SplitToSpaceLabel(field.Replace(lcTableName, "")
                                                            .Replace("Id", "")) + "</th>");
                                }

                                break;
                            }
                        }
                    }
                }

                template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                            </thead>");
                // detail load from xhr click. So no reload here 
                template.AppendLine("                            <tbody id=\"tableDetailBody\"></tbody>");
                template.AppendLine("                        </table>");

                // end table detail
                template.AppendLine("                    </div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </div>");
                template.AppendLine("        </div>");
                template.AppendLine("    </div>");
                template.AppendLine("    </section>");
                template.AppendLine("    <script>");
                // hmm seem missing here . validator  later
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }


                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (key.Equals("MUL"))
                    {
                        template.AppendLine(" var " + field.Replace("Id", "") + "Models = @Json.Serialize(" +
                                            field.Replace("Id", "") + "Models);");
                    }
                }

                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }


                    if (GetHiddenField().Any(x => field.Contains(x)) || !key.Equals("MUL")) continue;
                    if (!field.Equals(primaryKey))
                    {
                        template.AppendLine(" var " + field.Replace("Id", "") +
                                            "Models = @Json.Serialize(" +
                                            field.Replace("Id", "") + "Models);");
                    }
                }

                StringBuilder templateField = new();
                StringBuilder oneLineTemplateField = new();
                StringBuilder createTemplateField = new();
                //       StringBuilder updateTemplateField = new();


                StringBuilder templateFieldDetail = new();
                StringBuilder oneLineTemplateFieldDetail = new();
                StringBuilder createTemplateFieldDetail = new();
                //  StringBuilder updateTemplateFieldDetail = new();

                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                            case "MUL":
                                if (key.Equals("MUL"))
                                {
                                    templateField.Append("row." +
                                                         LowerCaseFirst(
                                                             GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                         ",");
                                    oneLineTemplateField.Append(
                                        LowerCaseFirst(GetLabelForComboBoxForGridOrOption(tableName, field)) + ",");
                                }
                                else
                                {
                                    templateField.Append("row." + field.Replace("Id", "Key") + ",");
                                    oneLineTemplateField.Append(field.Replace("Id", "Key") + ",");
                                }

                                break;
                            default:
                                templateField.Append("row." + field + ",");
                                oneLineTemplateField.Append(field + ",");
                                createTemplateField.Append(field + ".val(),");
                                break;
                        }
                    }
                }


                foreach (var describeTableModel in describeTableDetailModels
                        )
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                            case "MUL":
                                templateFieldDetail.Append("row." + field.Replace("Id", "Key") + ",");

                                oneLineTemplateFieldDetail.Append(field.Replace("Id", "Key") + ",");

                                if (!field.Equals(primaryKeyDetail))
                                {
                                    createTemplateFieldDetail.Append(field.Replace("Id", "Key") + ".val(),");
                                }

                                break;
                            default:
                                templateFieldDetail.Append("row." + field + ",");
                                oneLineTemplateFieldDetail.Append(field + ",");
                                createTemplateFieldDetail.Append(field + ".val(),");
                                break;
                        }
                    }
                }

                template.AppendLine("\tfunction newRecord() {");
                template.AppendLine("$(\"#listView\").hide();");
                template.AppendLine("$(\"#formView\").show();");
                template.AppendLine("\t}");

                template.AppendLine("\tfunction viewListRecord() {");
                template.AppendLine("$(\"#listView\").show();");
                template.AppendLine("$(\"#formView\").hide();");
                template.AppendLine("\t}");

                // reset form
                template.AppendLine("\tfunction resetForm() {");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                    {
                        name = fieldName;
                    }

                    if (name.Contains("Id"))
                    {
                        template.AppendLine("\t$(\"#" + name.Replace("Id", "Key") + "\").val('');");
                    }
                    else
                    {
                        template.AppendLine("\t$(\"#" + name + "\").val('');");
                    }
                }

                template.AppendLine("\t\t$(\"#createButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("\t\t$(\"#updateButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("\t\t$(\"#deleteButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("\t}");

                // reset record
                template.AppendLine("\tfunction resetRecord() {");
                template.AppendLine("\t\treadRecord();");
                template.AppendLine("\t\t$(\"#search\").val(\"\");");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                    {
                        name = fieldName;
                    }

                    if (name.Contains("Id"))
                    {
                        template.AppendLine("\t$(\"#" + name.Replace("Id", "Key") + "\").val('');");
                    }
                    else
                    {
                        template.AppendLine("\t$(\"#" + name + "\").val('');");
                    }
                }

                template.AppendLine("\t}");

                // empty template
                template.AppendLine("\tfunction emptyTemplate() {");
                template.AppendLine("\t\treturn\"<tr><td colspan='" + gridMax + "'>It's lonely here</td></tr>\";");
                template.AppendLine("\t}");

                template.AppendLine("\tfunction emptyDetailTemplate() {");
                template.AppendLine("\t\treturn\"<tr><td colspan='" + describeTableModels.Count +
                                    "'>It's lonely here</td></tr>\";");
                template.AppendLine("\t}");

                // start row master
                // read only max 6 
                template.AppendLine("        function template(" + oneLineTemplateField.ToString().TrimEnd(',') +
                                    ") {");

                template.AppendLine("            let template =  \"\" +");
                template.AppendLine($"                \"<tr id='{lcTableName}-\" + {lcTableName}Key + \"'>\" +");
                var m = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    continue;

                                case "MUL":
                                    template.AppendLine("\"<td>\"+" +
                                                        LowerCaseFirst(
                                                            GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                        "+\"</td>\" +");
                                    break;
                                default:
                                    template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                                    break;
                            }
                        }
                        else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                            else
                            {
                                template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                            }
                        }
                        else
                        {
                            template.AppendLine("\"<td>\"+" + LowerCaseFirst(field) + "+\"</td>\" +");
                        }

                        if (m == gridMax)
                        {
                            break;
                        }

                        m++;
                    }
                }

                template.AppendLine("                \"<td style='text-align: center'><div class='btn-group'>\" +");
                template.AppendLine(
                    $"                \"<Button type='button' class='btn btn-warning' onclick='viewRecord(\" + {lcTableName}Key + \")'>\" +");
                template.AppendLine("                \"<i class='fas fa-search'></i> View\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"</div></td>\" +");
                template.AppendLine("                \"</tr>\";");
                template.AppendLine("               return template; ");
                template.AppendLine("        }");
                // end row master

                // start row detail

                template.AppendLine("\tfunction templateDetail(" + oneLineTemplateFieldDetail.ToString().TrimEnd(',') +
                                    ") {");
                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;

                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x)) || !key.Equals("MUL") ||
                        field.Equals(primaryKey)) continue;
                    template.AppendLine("\tlet " + field.Replace("Id", "Key") + "Options = \"\";");
                    template.AppendLine("\tlet i = 0;");
                    template.AppendLine("\t" + field.Replace("Id", "") + "Models.map((row) => {");
                    template.AppendLine("\t\ti++;");
                    template.AppendLine("\t\tconst selected = (parseInt(row." +
                                        field.Replace("Id", "Key") +
                                        ") === parseInt(" + field.Replace("Id", "Key") +
                                        ")) ? \"selected\" : \"\";");
                    // @todo
                    template.AppendLine("\t\t" + field.Replace("Id", "Key") +
                                        "Options += \"<option value='\" + row." +
                                        field.Replace("Id", "Key") +
                                        " + \"' \" + selected + \">\"+row." +
                                        LowerCaseFirst(
                                            GetLabelForComboBoxForGridOrOption(tableNameDetail,
                                                field)) +
                                        "+\"</option>\";");
                    template.AppendLine("\t});");
                }

                template.AppendLine("\t\tlet template =  \"\" +");
                template.AppendLine(
                    $"                \"<tr id='{lcTableDetailName}-\" + {lcTableDetailName}Key + \"'>\" +");
                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            if (key.Equals("PRI") || field.Equals(primaryKey)) continue;
                            if (key.Equals("MUL"))
                            {
                                template.AppendLine("\t\t\"<td class='tdNormalAlign'>\" +");
                                template.AppendLine("\t\t\t\" <label>\" +");
                                template.AppendLine("\t\t\t\t\"<select id='" + field.Replace("Id", "Key") +
                                                    "-\"+" + lcTableDetailName +
                                                    "Key+\"' class='form-control'>\";");
                                template.AppendLine("\t\ttemplate += " + field.Replace("Id", "Key") +
                                                    "Options;");
                                template.AppendLine("\t\ttemplate += \"</select>\" +");
                                template.AppendLine("\t\t\"</label>\" +");
                                template.AppendLine("\t\t\"</td>\" +");
                            }
                            else
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("  \"<input type='number' name='" +
                                                    field.Replace("Id", "Key") + "' id='" +
                                                    field.Replace("Id", "Key") + "-\"+" +
                                                    lcTableDetailName + "Key+\"' value='\"+" +
                                                    LowerCaseFirst(field) +
                                                    "+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                        }
                        else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\"<td>\" +");
                            template.AppendLine(" \"<label>\" +");
                            template.AppendLine("   \"<input type='number' step='0.01' name='" + field + "' id='" +
                                                field + "-\"+" + lcTableDetailName + "Key+\"' value='\"+" +
                                                LowerCaseFirst(field) + "+\"' class='form-control' />\" +");
                            template.AppendLine(" \"</label>\" +");
                            template.AppendLine("\"</td>\" +");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='datetime-local' name='" + LowerCaseFirst(field) +
                                                    "' id='" + field.Replace("Id", "Key") + "-\"+" + lcTableDetailName +
                                                    "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    "+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='date' name='" + LowerCaseFirst(field) +
                                                    "'  id='" + field.Replace("Id", "Key") + "-\"+" +
                                                    lcTableDetailName + "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='time' name='" + LowerCaseFirst(field) +
                                                    "' id='" + field.Replace("Id", "Key") + "-\"+" + lcTableDetailName +
                                                    "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='number' min='1900' max='2099' step='1' name='" +
                                                    LowerCaseFirst(field) + "' id='" + field.Replace("Id", "Key") +
                                                    "-\"+" + lcTableDetailName + "Key+\"' value='\"+" +
                                                    LowerCaseFirst(field) +
                                                    ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("\"</td>\" +");
                            }
                            else
                            {
                                template.AppendLine("\"<td>\" +");
                                template.AppendLine(" \"<label>\" +");
                                template.AppendLine("   \"<input type='text' name='" + LowerCaseFirst(field) +
                                                    "' id='" + field.Replace("Id", "Key") + "-\"+" + lcTableDetailName +
                                                    "Key+\"' value='\"+" + LowerCaseFirst(field) +
                                                    "+\"' class='form-control' />\" +");
                                template.AppendLine(" \"</label>\" +");
                                template.AppendLine("                                   \"</td>\" +");
                            }
                        }
                        else
                        {
                            template.AppendLine("\"<td>\" +");
                            template.AppendLine(" \"<label>\" +");
                            template.AppendLine(" \"<input type='text' name='" + LowerCaseFirst(field) + "' id='" +
                                                field.Replace("Id", "Key") + "-\"+" + lcTableDetailName +
                                                "Key+\"'' value='\"+" + LowerCaseFirst(field) +
                                                "+\"' class='form-control' />\" +");
                            template.AppendLine(" \"</label>\" +");
                            template.AppendLine("\"</td>\" +");
                        }
                    }
                }

                template.AppendLine("\t\t\t\"<td style='text-align: center'><div class='btn-group'>\" +");
                if (primaryKeyDetail != null)
                {
                    template.AppendLine(
                        $"\t\t\t\" <Button type='button' class='btn btn-warning' onclick='updateDetailRecord(\" + {primaryKeyDetail.Replace("Id", "Key")} + \")'>\" +");
                }

                template.AppendLine("\t\t\t\t\"<i class='fas fa-edit'></i> UPDATE\" +");
                template.AppendLine("\t\t\t\"</Button>\" +");
                template.AppendLine("\t\t\t\"&nbsp;\" +");
                if (primaryKeyDetail != null)
                {
                    template.AppendLine(
                        $"\t\t\t\"<Button type='button' class='btn btn-danger' onclick='deleteDetailRecord(\" + {primaryKeyDetail.Replace("Id", "Key")} + \")'>\" +");
                }

                template.AppendLine("\t\t\t\t\"<i class='fas fa-trash'></i> DELETE\" +");
                template.AppendLine("\t\t\t\"</Button>\" +");
                template.AppendLine("\t\t\t\"</div></td>\" +");
                template.AppendLine("\t\t\t\"</tr>\";");
                template.AppendLine("\t\treturn template; ");
                template.AppendLine("\t}");
                // end template detail

                // if there is upload will be using form-data instead 
                if (imageUpload)
                {
                    template.AppendLine("        function createRecord() {");

                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    // do nothing
                                    break;
                                case "MUL":
                                    template.AppendLine(
                                        $" const {field.Replace("Id", "Key")} = $(\"#{field.Replace("Id", "Key")}\");");
                                    break;
                                default:
                                    template.AppendLine($" const {field} = $(\"#{field}\");");
                                    break;
                            }
                        }
                    }

                    template.AppendLine("\tvar formData = new FormData();");
                    template.AppendLine("\tformData.append(\"mode\",\"create\");");
                    template.AppendLine("\tformData.append(\"leafCheckKey\",@navigationModel.LeafCheckKey);");

                    // loop here 
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    template.AppendLine(
                                        $"\tformData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");
                                    break;
                                case "MUL":
                                {
                                    template.AppendLine(
                                        $"\tformData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");


                                    break;
                                }
                                default:
                                {
                                    if (GetNumberDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetDateDataType().Any(x => type.Contains(x)))
                                    {
                                        if (type.Contains("datetime"))
                                        {
                                            template.AppendLine(
                                                $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("date"))
                                        {
                                            template.AppendLine(
                                                $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("time"))
                                        {
                                            template.AppendLine(
                                                $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("year"))
                                        {
                                            template.AppendLine(
                                                $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else
                                        {
                                            template.AppendLine(
                                                $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                    }
                                    else if (GetBlobType().Any(x => type.Contains(x)))
                                    {
                                        // we check the size more then something ..
                                        template.AppendLine(
                                            $"var files{UpperCaseFirst(field)} = $('#{LowerCaseFirst(field)}')[0].files;");
                                        template.AppendLine("if(files" + UpperCaseFirst(field) + ".length > 0 ){");
                                        template.AppendLine("\tformData.append('" + LowerCaseFirst(field) + "',files" +
                                                            UpperCaseFirst(field) + "[0]);");
                                        template.AppendLine("}");
                                    }
                                    else
                                    {
                                        template.AppendLine(
                                            $"\tformData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    // loop here
                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("           url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("           async: false,");
                    template.AppendLine("           contentType: false,");
                    template.AppendLine("           processData: false, ");
                    template.AppendLine("           data: formData,");
                    template.AppendLine("           statusCode: {");
                    template.AppendLine("            500: function () {");
                    template.AppendLine(
                        "             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          },");
                    template.AppendLine("          beforeSend: function () {");
                    template.AppendLine("           console.log(\"loading ..\");");
                    template.AppendLine("          }}).done(function(data)  {");

                    template.AppendLine("            if (data === void 0) {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("            let status = data.status;");
                    template.AppendLine("            let code = data.code;");

                    template.AppendLine("            if (status) {");

                    template.AppendLine("             const lastInsertKey = data.lastInsertKey;");
                    // put value in input hidden
                    // flip create button disabled
                    // flip update button enabled
                    // flip disabled button enabled
                    if (primaryKey != null)
                    {
                        template.AppendLine("            $(\"#" + LowerCaseFirst(primaryKey.Replace("Id", "Key")) +
                                            "\").val(lastInsertKey);");
                    }

                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    // enable the detail button , we don't implement auto draft  here 
                    template.AppendLine(
                        "            $(\"#createDetailButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("               title: 'Success!',");
                    template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                    template.AppendLine("               icon: 'success',");
                    template.AppendLine("               confirmButtonText: 'Cool'");
                    template.AppendLine("             });");

                    template.AppendLine("            } else if (status === false) {");
                    template.AppendLine("             if (typeof(code) === 'string'){");
                    template.AppendLine("             @{");
                    template.AppendLine(
                        "              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }else{");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("                Swal.showLoading();");
                    template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("                timerInterval = setInterval(() => {");
                    template.AppendLine("                b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("               }, 100);");
                    template.AppendLine("              },");
                    template.AppendLine("              willClose: () => {");
                    template.AppendLine("               clearInterval(timerInterval);");
                    template.AppendLine("              }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("            });");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          } else {");
                    template.AppendLine("           location.href = \"/\";");
                    template.AppendLine("          }");
                    template.AppendLine("         }).fail(function(xhr)  {");
                    template.AppendLine("          console.log(xhr.status);");
                    template.AppendLine(
                        "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("         }).always(function (){");
                    template.AppendLine("          console.log(\"always:complete\");    ");
                    template.AppendLine("         });");
                    template.AppendLine("        }");
                }
                else
                {
                    template.AppendLine("        function createRecord() {");
                    // loop here 
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    continue;
                                case "MUL":
                                    template.AppendLine(
                                        " const " + field.Replace("Id", "Key") + " = $(\"#" +
                                        field.Replace("Id", "Key") + "\");");
                                    break;
                                default:
                                    template.AppendLine(" const " + field + " = $(\"#" + field + "\");");
                                    break;
                            }
                        }
                    }

                    // loop here
                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("           url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("           async: false,");
                    template.AppendLine("           data: {");
                    template.AppendLine("            mode: 'create',");
                    template.AppendLine("            leafCheckKey: @navigationModel.LeafCheckKey,");
                    // loop here
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":

                                    continue;
                                case "MUL":
                                    template.AppendLine($"            {field.Replace("Id", "Key")}: $(\"#" +
                                                        field.Replace("Id", "Key") + "\").val(),");
                                    break;
                                default:
                                    template.AppendLine($"            {field}: $(\"#" + field + "\").val(),");
                                    break;
                            }
                        }
                    }

                    // loop here
                    template.AppendLine("           },statusCode: {");
                    template.AppendLine("            500: function () {");
                    template.AppendLine(
                        " Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          },");
                    template.AppendLine("          beforeSend: function () {");
                    template.AppendLine("           console.log(\"loading ..\");");
                    template.AppendLine("          }}).done(function(data)  {");
                    template.AppendLine("            if (data === void 0) {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("            let status = data.status;");
                    template.AppendLine("            let code = data.code;");
                    template.AppendLine("            if (status) {");
                    template.AppendLine("             const lastInsertKey = data.lastInsertKey;");
                    // put value in input hidden
                    // flip create button disabled
                    // flip update button enabled
                    // flip disabled button enabled
                    if (primaryKey != null)
                    {
                        template.AppendLine("            $(\"#" + LowerCaseFirst(primaryKey.Replace("Id", "Key")) +
                                            "\").val(lastInsertKey);");
                    }

                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("               title: 'Success!',");
                    template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                    template.AppendLine("               icon: 'success',");
                    template.AppendLine("               confirmButtonText: 'Cool'");
                    template.AppendLine("             });");

                    template.AppendLine("            } else if (status === false) {");
                    template.AppendLine("             if (typeof(code) === 'string'){");
                    template.AppendLine("             @{");
                    template.AppendLine(
                        "              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }else{");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("                Swal.showLoading();");
                    template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("                timerInterval = setInterval(() => {");
                    template.AppendLine("                b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("               }, 100);");
                    template.AppendLine("              },");
                    template.AppendLine("              willClose: () => {");
                    template.AppendLine("               clearInterval(timerInterval);");
                    template.AppendLine("              }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("            });");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          } else {");
                    template.AppendLine("           location.href = \"/\";");
                    template.AppendLine("          }");
                    template.AppendLine("         }).fail(function(xhr)  {");
                    template.AppendLine("          console.log(xhr.status);");
                    template.AppendLine(
                        "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("         }).always(function (){");
                    template.AppendLine("          console.log(\"always:complete\");    ");
                    template.AppendLine("         });");
                    template.AppendLine("        }");
                }

                // create record detail
                template.AppendLine("        function createDetailRecord() {");
                // loop here
                // @todo  should refer this . should be refer mul and pri key
                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                            case "MUL":
                                if (field.Equals(primaryKey))
                                {
                                    template.AppendLine(
                                        $"\t\tconst {field.Replace("Id", "Key")} = $(\"#{field.Replace("Id", "Key")}\");");
                                }
                                else
                                {
                                    if (!field.Equals(primaryKeyDetail))
                                    {
                                        template.AppendLine(
                                            $"\t\tconst {field.Replace("Id", "Key")} = $(\"#detail_{field.Replace("Id", "Key")}\");");
                                    }
                                }

                                break;
                            default:
                                template.AppendLine($"\t\tconst {field} = $(\"#detail_{field}\");");

                                break;
                        }
                    }
                }

                // loop here
                template.AppendLine("\t\t$.ajax({");
                template.AppendLine("\t\t\ttype: 'POST',");
                template.AppendLine("\t\t\turl: \"api/" + module.ToLower() + "/" + lcTableDetailName + "\",");
                template.AppendLine("\t\t\tasync: false,");
                template.AppendLine("\t\t\tdata: {");
                template.AppendLine("\t\t\t\tmode: 'create',");
                template.AppendLine("\t\t\t\tleafCheckKey: @navigationModel.LeafCheckKey,");
                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                            case "MUL":
                                if (!field.Equals(primaryKeyDetail))
                                {
                                    template.AppendLine(
                                        $"\t\t\t\t{field.Replace("Id", "Key")}: {field.Replace("Id", "Key")}.val(),");
                                }

                                break;
                            default:
                                template.AppendLine($"\t\t\t\t{field}: {field}.val(),");

                                break;
                        }
                    }
                }

                // loop here
                template.AppendLine("\t\t\t},statusCode: {");
                template.AppendLine("\t\t\t\t500: function () {");
                template.AppendLine(
                    "\t\t\t\t\tSwal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          },");
                template.AppendLine("          beforeSend: function () {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("            if (data === void 0) {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("            let status = data.status;");
                template.AppendLine("            let code = data.code;");
                template.AppendLine("            if (status) {");
                template.AppendLine("             const lastInsertKey = data.lastInsertKey;");
                template.AppendLine("             $(\"#tableDetailBody\").prepend(templateDetail(lastInsertKey," +
                                    createTemplateFieldDetail.ToString().TrimEnd(',') + "));");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("               title: 'Success!',");
                template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                template.AppendLine("               icon: 'success',");
                template.AppendLine("               confirmButtonText: 'Cool'");
                template.AppendLine("             });");
                // loop here
                foreach (var describeTableModel in describeTableDetailModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                            case "MUL":
                                if (!field.Equals(primaryKey))
                                {
                                    if (!field.Equals(primaryKeyDetail))
                                    {
                                        template.AppendLine($"\t{field.Replace("Id", "Key")}.val('');");
                                    }
                                }

                                break;
                            default:
                                template.AppendLine("\t" + field + ".val('');");
                                break;
                        }
                    }
                }

                // loop here
                template.AppendLine("            } else if (status === false) {");
                template.AppendLine("             if (typeof(code) === 'string'){");
                template.AppendLine("             @{");
                template.AppendLine(
                    "              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }else{");
                template.AppendLine("               <text>");
                template.AppendLine(
                    "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine(
                    "            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval=0;");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine(
                    "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("                Swal.showLoading();");
                template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                timerInterval = setInterval(() => {");
                template.AppendLine("                b.textContent = Swal.getTimerLeft();");
                template.AppendLine("               }, 100);");
                template.AppendLine("              },");
                template.AppendLine("              willClose: () => {");
                template.AppendLine("               clearInterval(timerInterval);");
                template.AppendLine("              }");
                template.AppendLine("            }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("            });");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          } else {");
                template.AppendLine("           location.href = \"/\";");
                template.AppendLine("          }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("          console.log(xhr.status);");
                template.AppendLine(
                    "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");


                // read record
                template.AppendLine("        function readRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"read\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) {");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                template.AppendLine("               if (data.data === void 0) {");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("               } else {");
                template.AppendLine("                if (data.data.length > 0) {");
                template.AppendLine("                 let templateStringBuilder = \"\";");
                template.AppendLine("                 for (let i = 0; i < data.data.length; i++) {");
                template.AppendLine("                  let row = data.data[i];");
                // remember one line row 
                template.AppendLine("                  templateStringBuilder += template(" +
                                    templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                } else {");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("              }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("              });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");

                // read record detail
                template.AppendLine("        function readDetailRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableDetailName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"read\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) {");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                template.AppendLine("               if (data.data === void 0) {");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("               } else {");
                template.AppendLine("                if (data.data.length > 0) {");
                template.AppendLine("                 let templateStringBuilder = \"\";");
                template.AppendLine("                 for (let i = 0; i < data.data.length; i++) {");
                template.AppendLine("                  let row = data.data[i];");
                // remember one line row 
                template.AppendLine("                  templateStringBuilder += template(" +
                                    templateFieldDetail.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableDetailBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                } else {");
                template.AppendLine("                 $(\"#tableDetailBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("              }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("              });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");

                // search record
                template.AppendLine("        function searchRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"search\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("           search: $(\"#search\").val()");
                template.AppendLine("          }, ");
                template.AppendLine("          statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("            if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("             let status =data.status;");
                template.AppendLine("             let code = data.code;");
                template.AppendLine("             if (status) {");
                template.AppendLine("              if (data.data === void 0) {");
                template.AppendLine("               $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("              } else {");
                template.AppendLine("               if (data.data.length > 0) {");
                template.AppendLine("                let templateStringBuilder = \"\";");
                template.AppendLine("                for (let i = 0; i < data.data.length; i++) {");
                template.AppendLine("                 let row = data.data[i];");
                template.AppendLine("                 templateStringBuilder += template(" +
                                    templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                }");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("               }");
                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval); }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("          console.log(xhr.status);");
                template.AppendLine(
                    "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");

                // excel record
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableName + "\");");
                template.AppendLine("        }");

                // view record detail 
                if (primaryKey != null)
                {
                    template.AppendLine("        function viewRecord(" +
                                        LowerCaseFirst(primaryKey.Replace("Id", "Key")) + ") {");
                }

                // @todo open first the layout . maybe shade first loading but sometimes end user don't think much. a few second maybe ?
                // we prefer not jumping layout just for loading purpose .. annoy
                template.AppendLine("$(\"#listView\").hide();");
                template.AppendLine("$(\"#formView\").show();");

                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"single\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                if (primaryKey != null)
                {
                    template.AppendLine("           " + LowerCaseFirst(primaryKey.Replace("Id", "Key")) + ": " +
                                        LowerCaseFirst(primaryKey.Replace("Id", "Key")));
                }

                template.AppendLine("          }, ");
                template.AppendLine("          statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          }, beforeSend() {");
                template.AppendLine("           console.log(\"loading ..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("            if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("             let status =data.status;");
                template.AppendLine("             let code = data.code;");
                template.AppendLine("             if (status) {");
                template.AppendLine("              if (data.dataSingle === void 0) {");
                template.AppendLine("               $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("              } else {");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                case "MUL":
                                    template.AppendLine("\t$(\"#" + LowerCaseFirst(field.Replace("Id", "Key")) +
                                                        "\").val(data.dataSingle." +
                                                        LowerCaseFirst(field.Replace("Id", "Key")) + ");");
                                    break;
                                default:
                                    template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                        LowerCaseFirst(field) + ");");
                                    break;
                            }
                        }
                        else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                LowerCaseFirst(field) + ");");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                            else
                            {
                                template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                    LowerCaseFirst(field) + ");");
                            }
                        }
                        else if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("\t$(\"#" + LowerCaseFirst(field) +
                                                "Image\").attr(\"src\",data.dataSingle." + LowerCaseFirst(field) +
                                                "Base64String);");
                        }
                        else
                        {
                            template.AppendLine("\t$(\"#" + LowerCaseFirst(field) + "\").val(data.dataSingle." +
                                                LowerCaseFirst(field) + ");");
                        }
                    }
                }

                template.AppendLine("                if (data.dataSingle.data.length > 0) {");
                template.AppendLine("                 let templateStringBuilder = \"\";");
                template.AppendLine("                 for (let i = 0; i < data.dataSingle.data.length; i++) {");
                template.AppendLine("                  let row = data.dataSingle.data[i];");
                // remember one line row 
                template.AppendLine("                  templateStringBuilder += templateDetail(" +
                                    templateFieldDetail.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableDetailBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                 }");
                template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"html, body\").animate({ scrollTop: 0 }, \"slow\");");

                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval); }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("          console.log(xhr.status);");
                template.AppendLine(
                    "          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");

                // excel record
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableName + "\");");
                template.AppendLine("        }");

                // update record
                if (imageUpload)
                {
                    template.AppendLine("        function updateRecord() {");
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }


                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                case "MUL":
                                    template.AppendLine(
                                        $" const {field.Replace("Id", "Key")} = $(\"#{field.Replace("Id", "Key")}\");");
                                    break;
                                default:
                                    template.AppendLine($" const {field} = $(\"#{field}\");");

                                    break;
                            }
                        }
                    }

                    template.AppendLine(" var formData = new FormData();");
                    template.AppendLine(" formData.append(\"mode\",\"update\");");
                    template.AppendLine(" formData.append(\"leafCheckKey\",@navigationModel.LeafCheckKey);");

                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                    template.AppendLine(
                                        $"        formData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");
                                    break;
                                case "MUL":
                                {
                                    template.AppendLine(
                                        $"        formData.append('{LowerCaseFirst(field.Replace("Id", "Key"))}',{LowerCaseFirst(field.Replace("Id", "Key"))}.val());");


                                    break;
                                }
                                default:
                                {
                                    if (GetNumberDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetNumberDotDataType().Any(x => type.Contains(x)))
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }
                                    else if (GetDateDataType().Any(x => type.Contains(x)))
                                    {
                                        if (type.Contains("datetime"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("date"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("time"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else if (type.Contains("year"))
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                        else
                                        {
                                            template.AppendLine(
                                                $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                        }
                                    }
                                    else if (GetBlobType().Any(x => type.Contains(x)))
                                    {
                                        // we check the size more then something ..
                                        template.AppendLine(
                                            $"var files{UpperCaseFirst(field)} = $('#{LowerCaseFirst(field)}')[0].files;");
                                        template.AppendLine("if(files" + UpperCaseFirst(field) + ".length > 0 ){");
                                        template.AppendLine("        formData.append('" + LowerCaseFirst(field) +
                                                            "',files" +
                                                            UpperCaseFirst(field) + "[0]);");
                                        template.AppendLine("}");
                                    }
                                    else
                                    {
                                        template.AppendLine(
                                            $"        formData.append('{LowerCaseFirst(field)}',{LowerCaseFirst(field)}.val());");
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("           async: false,");
                    template.AppendLine("           contentType: false,");
                    template.AppendLine("           processData: false, ");
                    template.AppendLine("           data: formData,");
                    template.AppendLine("          statusCode: {");
                    template.AppendLine("           500: function () {");
                    template.AppendLine(
                        "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          }, beforeSend() {");
                    template.AppendLine("           console.log(\"loading..\");");
                    template.AppendLine("          }}).done(function(data)  {");
                    template.AppendLine("           if (data === void 0) {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("           let status = data.status;");
                    template.AppendLine("           let code = data.code;");
                    template.AppendLine("           if (status) {");
                    // flip the update button enabled
                    // flip the delete button enable
                    // flip the create button disabled
                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success');");
                    template.AppendLine("           } else if (status === false) {");
                    template.AppendLine("            if (typeof(code) === 'string'){");
                    template.AppendLine("            @{");
                    template.AppendLine(
                        "             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("              else");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("              Swal.showLoading();");
                    template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("               timerInterval = setInterval(() => {");
                    template.AppendLine("               b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("              }, 100);");
                    template.AppendLine("             },");
                    template.AppendLine("             willClose: () => {");
                    template.AppendLine("              clearInterval(timerInterval);");
                    template.AppendLine("             }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("             });");
                    template.AppendLine("            } else {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          }).fail(function(xhr)  {");
                    template.AppendLine("           console.log(xhr.status);");
                    template.AppendLine(
                        "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("          }).always(function (){");
                    template.AppendLine("           console.log(\"always:complete\");    ");
                    template.AppendLine("          });");
                    template.AppendLine("        }");
                }
                else
                {
                    template.AppendLine("        function updateRecord() {");
                    template.AppendLine("         $.ajax({");
                    template.AppendLine("          type: 'POST',");
                    template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                    template.AppendLine("          async: false,");
                    template.AppendLine("          data: {");
                    template.AppendLine("           mode: 'update',");
                    template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                    // loop here
                    template.AppendLine("             " + lcTableName + "Key: $(\"#" + lcTableName + "Key\").val(),");
                    // loop not primary
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (!GetHiddenField().Any(x => field.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                case "MUL":
                                    template.AppendLine($"            {field.Replace("Id", "Key")}: $(\"#" +
                                                        field.Replace("Id", "Key") + "\").val(),");
                                    break;
                                default:
                                    template.AppendLine($"            {field}: $(\"#" + field + "\").val(),");
                                    break;
                            }
                        }
                    }

                    // loop here
                    template.AppendLine("          }, statusCode: {");
                    template.AppendLine("           500: function () {");
                    template.AppendLine(
                        "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("           }");
                    template.AppendLine("          },");
                    template.AppendLine("          beforeSend: function () {");
                    template.AppendLine("           console.log(\"loading..\");");
                    template.AppendLine("          }}).done(function(data)  {");
                    template.AppendLine("           if (data === void 0) {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("           let status = data.status;");
                    template.AppendLine("           let code = data.code;");
                    template.AppendLine("           if (status) {");
                    // flip the update button enabled
                    // flip the delete button enable
                    // flip the create button disabled
                    template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#updateButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            $(\"#deleteButton\").removeAttr(\"disabled\",\"disabled\");");
                    template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success');");
                    template.AppendLine("           } else if (status === false) {");
                    template.AppendLine("            if (typeof(code) === 'string'){");
                    template.AppendLine("            @{");
                    template.AppendLine(
                        "             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("              else");
                    template.AppendLine("              {");
                    template.AppendLine("               <text>");
                    template.AppendLine(
                        "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("               </text>");
                    template.AppendLine("              }");
                    template.AppendLine("             }");
                    template.AppendLine(
                        "            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                    template.AppendLine("             let timerInterval=0;");
                    template.AppendLine("             Swal.fire({");
                    template.AppendLine("              title: 'Auto close alert!',");
                    template.AppendLine(
                        "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                    template.AppendLine("              timer: 2000,");
                    template.AppendLine("              timerProgressBar: true,");
                    template.AppendLine("              didOpen: () => {");
                    template.AppendLine("              Swal.showLoading();");
                    template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                    template.AppendLine("               timerInterval = setInterval(() => {");
                    template.AppendLine("               b.textContent = Swal.getTimerLeft();");
                    template.AppendLine("              }, 100);");
                    template.AppendLine("             },");
                    template.AppendLine("             willClose: () => {");
                    template.AppendLine("              clearInterval(timerInterval);");
                    template.AppendLine("             }");
                    template.AppendLine("            }).then((result) => {");
                    template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                    template.AppendLine("               console.log('session out .. ');");
                    template.AppendLine("               location.href = \"/\";");
                    template.AppendLine("              }");
                    template.AppendLine("             });");
                    template.AppendLine("            } else {");
                    template.AppendLine("             location.href = \"/\";");
                    template.AppendLine("            }");
                    template.AppendLine("           } else {");
                    template.AppendLine("            location.href = \"/\";");
                    template.AppendLine("           }");
                    template.AppendLine("          }).fail(function(xhr)  {");
                    template.AppendLine("           console.log(xhr.status);");
                    template.AppendLine(
                        "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                    template.AppendLine("          }).always(function (){");
                    template.AppendLine("           console.log(\"always:complete\");    ");
                    template.AppendLine("          });");
                    template.AppendLine("        }");
                }

                // update record detail
                if (primaryKeyDetail != null)
                {
                    template.AppendLine("        function updateDetailRecord(" + primaryKeyDetail.Replace("Id", "Key") +
                                        ") {");
                }

                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: 'POST',");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableDetailName + "\",");

                template.AppendLine("          async: false,");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: 'update',");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                // loop here
                if (primaryKeyDetail != null)
                {
                    template.AppendLine("           " + primaryKeyDetail.Replace("Id", "Key") + ": " +
                                        primaryKeyDetail.Replace("Id", "Key") + ",");
                }

                // loop not primary
                foreach (var describeTableModel in describeTableDetailModels
                        )
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                continue;
                            case "MUL":
                                if (!field.Equals(primaryKey))
                                {
                                    if (primaryKeyDetail != null)
                                    {
                                        template.AppendLine(
                                            $"           {field.Replace("Id", "Key")}: $(\"#{field.Replace("Id", "Key")}-\" + {primaryKeyDetail.Replace("Id", "Key")}).val(),");
                                    }
                                }
                                else
                                {
                                    template.AppendLine("           " + primaryKey.Replace("Id", "Key") + ": $(\"#" +
                                                        primaryKey.Replace("Id", "Key") + "\").val(),");
                                }

                                break;
                            default:
                                if (primaryKeyDetail != null)
                                {
                                    template.AppendLine(
                                        $"           {field}: $(\"#{field}-\" + {primaryKeyDetail.Replace("Id", "Key")}).val(),");
                                }

                                break;
                        }
                    }
                }

                // loop here
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine(
                    "            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("           }");
                template.AppendLine("          },");
                template.AppendLine("          beforeSend: function () {");
                template.AppendLine("           console.log(\"loading..\");");
                template.AppendLine("          }}).done(function(data)  {");
                template.AppendLine("           if (data === void 0) {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("           let status = data.status;");
                template.AppendLine("           let code = data.code;");
                template.AppendLine("           if (status) {");
                template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success');");
                template.AppendLine("           } else if (status === false) {");
                template.AppendLine("            if (typeof(code) === 'string'){");
                template.AppendLine("            @{");
                template.AppendLine(
                    "             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("              else");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine(
                    "                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine(
                    "            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval=0;");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine(
                    "              html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("              Swal.showLoading();");
                template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("               timerInterval = setInterval(() => {");
                template.AppendLine("               b.textContent = Swal.getTimerLeft();");
                template.AppendLine("              }, 100);");
                template.AppendLine("             },");
                template.AppendLine("             willClose: () => {");
                template.AppendLine("              clearInterval(timerInterval);");
                template.AppendLine("             }");
                template.AppendLine("            }).then((result) => {");
                template.AppendLine("              if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("               console.log('session out .. ');");
                template.AppendLine("               location.href = \"/\";");
                template.AppendLine("              }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("          }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");

                // delete record
                template.AppendLine("        function deleteRecord() { ");
                template.AppendLine("         Swal.fire({");
                template.AppendLine("          title: 'Are you sure?',");
                template.AppendLine("          text: \"You won't be able to revert this!\",");
                template.AppendLine("          type: 'warning',");
                template.AppendLine("          showCancelButton: true,");
                template.AppendLine("          confirmButtonText: 'Yes, delete it!',");
                template.AppendLine("          cancelButtonText: 'No, cancel!',");
                template.AppendLine("          reverseButtons: true");
                template.AppendLine("         }).then((result) => {");
                template.AppendLine("          if (result.value) {");
                template.AppendLine("           $.ajax({");
                template.AppendLine("            type: 'POST',");
                template.AppendLine("            url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("            async: false,");
                template.AppendLine("            data: {");
                template.AppendLine("             mode: 'delete',");
                template.AppendLine("             leafCheckKey: @navigationModel.LeafCheckKey,");
                if (primaryKey != null)
                {
                    template.AppendLine("             " + LowerCaseFirst(primaryKey.Replace("Id", "Key")) + ": $(\"#" +
                                        LowerCaseFirst(primaryKey.Replace("Id", "Key")) + "\").val()");
                }

                template.AppendLine("            }, statusCode: {");
                template.AppendLine("             500: function () {");
                template.AppendLine(
                    "              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("             }");
                template.AppendLine("            },");
                template.AppendLine("            beforeSend: function () {");
                template.AppendLine("             console.log(\"loading..\");");
                template.AppendLine("           }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                // reset hidden key primary key
                // flip the update button disabled
                // flip the delete button disabled
                // flip the create button enabled
                // reset all record 
                template.AppendLine("            $(\"#createButton\").removeAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").attr(\"disabled\",\"disabled\");");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                            case "MUL":
                                template.AppendLine("\t$(\"#" + field.Replace("Id", "Key") + "\").val('');");

                                break;
                            default:
                                template.AppendLine("\t$(\"#" + field + "\").val('');");

                                break;
                        }
                    }
                }

                // empty the grid row 
                template.AppendLine("               $(\"#createDetailButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("               $(\"#tableDetailBody\").html(\"\");");
                // disable the grid row button create
                template.AppendLine(
                    "               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("       } else if (result.dismiss === swal.DismissReason.cancel) {");
                template.AppendLine("        Swal.fire({");
                template.AppendLine("          icon: 'error',");
                template.AppendLine("          title: 'Cancelled',");
                template.AppendLine("          text: 'Be careful before delete record'");
                template.AppendLine("        })");
                template.AppendLine("       }");
                template.AppendLine("      });");
                template.AppendLine("    }");

                // delete record detail
                template.AppendLine("        function deleteDetailRecord(" + lcTableDetailName + "Key) { ");
                template.AppendLine("         Swal.fire({");
                template.AppendLine("          title: 'Are you sure?',");
                template.AppendLine("          text: \"You won't be able to revert this!\",");
                template.AppendLine("          type: 'warning',");
                template.AppendLine("          showCancelButton: true,");
                template.AppendLine("          confirmButtonText: 'Yes, delete it!',");
                template.AppendLine("          cancelButtonText: 'No, cancel!',");
                template.AppendLine("          reverseButtons: true");
                template.AppendLine("         }).then((result) => {");
                template.AppendLine("          if (result.value) {");
                template.AppendLine("           $.ajax({");
                template.AppendLine("            type: 'POST',");
                template.AppendLine("            url: \"api/" + module.ToLower() + "/" + lcTableDetailName + "\",");
                template.AppendLine("            async: false,");
                template.AppendLine("            data: {");
                template.AppendLine("             mode: 'delete',");
                template.AppendLine("             leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("             " + lcTableDetailName + "Key: " + lcTableDetailName + "Key");
                template.AppendLine("            }, statusCode: {");
                template.AppendLine("             500: function () {");
                template.AppendLine(
                    "              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("             }");
                template.AppendLine("            },");
                template.AppendLine("            beforeSend: function () {");
                template.AppendLine("             console.log(\"loading..\");");
                template.AppendLine("           }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                template.AppendLine("               $(\"#" + lcTableDetailName + "-\" + " + lcTableDetailName +
                                    "Key).remove();");
                template.AppendLine(
                    "               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine(
                    "                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine(
                    "                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine(
                    "              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval=0;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine(
                    "                html: 'Session Out .Please Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft();");
                template.AppendLine("                }, 100);");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval);");
                template.AppendLine("               }");
                template.AppendLine("             }).then((result) => {");
                template.AppendLine("               if (result.dismiss === Swal.DismissReason.timer) {");
                template.AppendLine("                console.log('session out .. ');");
                template.AppendLine("                location.href = \"/\";");
                template.AppendLine("               }");
                template.AppendLine("             });");
                template.AppendLine("            } else {");
                template.AppendLine("             location.href = \"/\";");
                template.AppendLine("            }");
                template.AppendLine("           } else {");
                template.AppendLine("            location.href = \"/\";");
                template.AppendLine("           }");
                template.AppendLine("         }).fail(function(xhr)  {");
                template.AppendLine("           console.log(xhr.status);");
                template.AppendLine(
                    "           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("       } else if (result.dismiss === swal.DismissReason.cancel) {");
                template.AppendLine("        Swal.fire({");
                template.AppendLine("          icon: 'error',");
                template.AppendLine("          title: 'Cancelled',");
                template.AppendLine("          text: 'Be careful before delete record'");
                template.AppendLine("        })");
                template.AppendLine("       }");
                template.AppendLine("      });");
                template.AppendLine("    }");
                template.AppendLine("    </script>");


                return template.ToString();
            }

            public string GenerateRepository(string module, string tableName, string tableNameDetail = "",
                bool readOnly = false)
            {
                var primaryKey = GetPrimaryKeyTableName(tableName);

                var ucTableName = GetStringNoUnderScore(tableName, (int) TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int) TextCase.LcWords);

                var ucTableNameDetail = string.Empty;
                var lcTableNameDetail = string.Empty;
                if (!string.IsNullOrEmpty(tableNameDetail))
                {
                    ucTableNameDetail = GetStringNoUnderScore(tableNameDetail, (int) TextCase.UcWords);
                    lcTableNameDetail = GetStringNoUnderScore(tableNameDetail, (int) TextCase.LcWords);
                }

                var isDeleteFieldExisted = GetIfExistedIsDeleteField(tableName);

                var describeTableModels = GetTableStructure(tableName);
                List<DescribeTableModel> describeTableDetailModels = new();
                if (!string.IsNullOrEmpty(tableNameDetail))
                {
                    describeTableDetailModels = GetTableStructure(tableNameDetail);
                }

                var fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();

                var sqlFieldName = string.Join(',', fieldNameList);
                List<string?> fieldNameParameter = new();
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }


                    switch (key)
                    {
                        case "PRI":
                            fieldNameParameter.Add("null");

                            break;
                        default:
                            fieldNameParameter.Add("@" + field);

                            break;
                    }
                }

                var sqlBindParamFieldName = string.Join(',', fieldNameParameter);

                StringBuilder loopColumn = new();
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    List<string> keyValue = new() {"PRI", "MUL"};
                    if (keyValue.Contains(key))
                    {
                        if (key.Equals("PRI")) continue;
                        if (field.Equals("tenantId"))
                        {
                            loopColumn.AppendLine("                    new ()");
                            loopColumn.AppendLine("                    {");
                            loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                            loopColumn.AppendLine("                        Value = _sharedUtil.GetTenantId()");
                            loopColumn.AppendLine("                    },");
                        }
                        else
                        {
                            loopColumn.AppendLine("                    new ()");
                            loopColumn.AppendLine("                    {");
                            loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                            loopColumn.AppendLine("                        Value = " + lcTableName + "Model." +
                                                  UpperCaseFirst(field.Replace("Id", "Key")));
                            loopColumn.AppendLine("                    },");
                        }
                    }
                    else
                    {
                        switch (field)
                        {
                            case "isDelete":
                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                                loopColumn.AppendLine("                        Value = 0");
                                loopColumn.AppendLine("                    },");
                                break;
                            case "executeBy":
                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                                loopColumn.AppendLine("                        Value = _sharedUtil.GetUserName()");
                                loopColumn.AppendLine("                    },");
                                break;
                            default:
                            {
                                if (type.Contains("datetime"))
                                {
                                    loopColumn.AppendLine("                    new ()");
                                    loopColumn.AppendLine("                    {");
                                    loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                                    loopColumn.AppendLine("                        Value = " + lcTableName + "Model." +
                                                          UpperCaseFirst(field) + "?.ToString(\"yyyy-MM-dd HH:mm\")");
                                    loopColumn.AppendLine("                    },");
                                }
                                else if (type.Contains("date"))
                                {
                                    loopColumn.AppendLine("                    new ()");
                                    loopColumn.AppendLine("                    {");
                                    loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                                    loopColumn.AppendLine("                        Value = " + lcTableName + "Model." +
                                                          UpperCaseFirst(field) + "?.ToString(\"yyyy-MM-dd\")");
                                    loopColumn.AppendLine("                    },");
                                }
                                else if (type.Contains("time"))
                                {
                                    loopColumn.AppendLine("                    new ()");
                                    loopColumn.AppendLine("                    {");
                                    loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                                    loopColumn.AppendLine("                        Value = " + lcTableName + "Model." +
                                                          UpperCaseFirst(field) + "?.ToString(\"HH:mm\")");
                                    loopColumn.AppendLine("                    },");
                                }
                                else
                                {
                                    loopColumn.AppendLine("                    new ()");
                                    loopColumn.AppendLine("                    {");
                                    loopColumn.AppendLine("                        Key = \"@" + field + "\",");
                                    loopColumn.AppendLine("                        Value = " + lcTableName + "Model." +
                                                          UpperCaseFirst(field));
                                    loopColumn.AppendLine("                    },");
                                }

                                break;
                            }
                        }
                    }
                }


                StringBuilder template = new();
                template.AppendLine("using ClosedXML.Excel;");
                template.AppendLine("using MySql.Data.MySqlClient;");
                template.AppendLine("using RebelCmsTemplate.Models." + module + ";");
                template.AppendLine("using RebelCmsTemplate.Models.Shared;");
                template.AppendLine("using RebelCmsTemplate.Util;");

                template.AppendLine("namespace RebelCmsTemplate.Repository." + module + ";");
                template.AppendLine("    public class " + ucTableName + "Repository");
                template.AppendLine("    {");
                template.AppendLine("        private readonly SharedUtil _sharedUtil;");

                template.AppendLine("        public " + ucTableName +
                                    "Repository(IHttpContextAccessor httpContextAccessor)");
                template.AppendLine("        {");
                template.AppendLine("            _sharedUtil = new SharedUtil(httpContextAccessor);");
                template.AppendLine("        }");

                template.AppendLine("        public int Create(" + ucTableName + "Model " + lcTableName + "Model)");
                template.AppendLine("        {");
                template.AppendLine("            int lastInsertKey;");
                template.AppendLine("            var query = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine(
                    "                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                template.Append("                sql = @\"");
                template.Append("                INSERT INTO " + tableName + " (" + sqlFieldName + ") VALUES (" +
                                sqlBindParamFieldName + ");");
                template.AppendLine("\";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(query, connection);");
                template.AppendLine("                parameterModels = new List<ParameterModel>");

                template.AppendLine("                {");
                // loop start
                template.AppendLine(loopColumn.ToString().TrimEnd(','));
                // loop end
                template.AppendLine("                };");
                template.AppendLine("                foreach (var parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine(
                    "                   mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                template.AppendLine("                mySqlTransaction.Commit();");
                template.AppendLine("                lastInsertKey = (int)mySqlCommand.LastInsertedId;");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine(
                    "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");
                template.AppendLine("            return lastInsertKey;");

                template.AppendLine("        }");


                template.AppendLine("        public List<" + ucTableName + "Model> Read()");
                template.AppendLine("        {");
                template.AppendLine("            List<" + ucTableName + "Model> " + lcTableName + "Models = new();");
                template.AppendLine("            var sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine("                sql = @\"");
                template.AppendLine("                SELECT      *");
                template.AppendLine("                FROM        " + tableName + " ");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }


                    if (GetHiddenField().Any(x => field.Contains(x)) ||
                        !GetNumberDataType().Any(x => type.Contains(x)) || !key.Equals("MUL")) continue;
                    template.AppendLine("\t JOIN " + GetForeignKeyTableName(tableName, field) + " ");
                    template.AppendLine("\t USING(" + field + ")");
                }

                if (isDeleteFieldExisted)
                {
                    template.AppendLine($"\t WHERE   {tableName}.isDelete != 1");
                }
                else
                {
                    template.AppendLine(" WHERE 1 ");
                }

                template.AppendLine("                ORDER BY    " + primaryKey + " DESC \";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    while (reader.Read())");
                template.AppendLine("                    {");

                template.AppendLine(
                    "                        " + lcTableName + "Models.Add(new " + ucTableName + "Model");
                template.AppendLine("                       {");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            if (key.Equals("PRI") || key.Equals("MUL"))
                            {
                                if (readOnly && key.Equals("MUL"))
                                {
                                    template.AppendLine(" " +
                                                        UpperCaseFirst(
                                                            GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                        " = reader[\"" +
                                                        LowerCaseFirst(
                                                            GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                        "\"].ToString(),");
                                }

                                template.AppendLine("                            " +
                                                    UpperCaseFirst(field.Replace("Id", "Key") +
                                                                   " = Convert.ToInt32(reader[\"" +
                                                                   LowerCaseFirst(field)) + "\"]),");
                            }
                            else
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = Convert.ToInt32(reader[\"" + LowerCaseFirst(field) + "\"]),");
                            }
                        }
                        else if (GetDoubleDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = Convert.ToDouble(reader[\"" + LowerCaseFirst(field) + "\"]),");
                        }
                        else if (type.Contains("decimal"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = Convert.ToDecimal(reader[\"" + LowerCaseFirst(field) + "\"]),");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = Convert.ToDateTime(reader[\"" + LowerCaseFirst(field) +
                                                    "\"]),");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = Convert.ToInt32(reader[\"" + LowerCaseFirst(field) + "\"]),");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = (reader[\"" + LowerCaseFirst(field) +
                                                    "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToTime((DateTime)reader[\"" +
                                                    LowerCaseFirst(field) + "\"]): null,");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = (reader[\"" + LowerCaseFirst(field) +
                                                    "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" +
                                                    LowerCaseFirst(field) + "\"]): null,");
                            }
                        }
                        else if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = (byte[])reader[\"" + LowerCaseFirst(field) + "\"],");
                        }
                        else
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = reader[\"" + LowerCaseFirst(field) + "\"].ToString(),");
                        }
                    }
                }

                template.AppendLine("});");
                template.AppendLine("                    }");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine(
                    "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("               throw new Exception(ex.Message);");
                template.AppendLine("            }");

                template.AppendLine("            return " + lcTableName + "Models;");
                template.AppendLine("        }");
                // search method/function
                template.AppendLine("        public List<" + ucTableName + "Model> Search(string search)");
                template.AppendLine("       {");
                template.AppendLine("            List<" + ucTableName + "Model> " + lcTableName + "Models = new();");
                template.AppendLine("            var sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                // the main problem is are we should filter em all or else ? 
                template.AppendLine("                connection.Open();");
                template.AppendLine("                sql += @\"");
                template.AppendLine("                SELECT  *");
                template.AppendLine("                FROM    " + tableName + " ");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }


                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            switch (key)
                            {
                                case "MUL":
                                    template.AppendLine("\t JOIN " + GetForeignKeyTableName(tableName, field) + " ");
                                    template.AppendLine("\t USING(" + field + ")");
                                    break;
                            }
                        }
                    }
                }

                // how many table join is related ?
                if (isDeleteFieldExisted)
                {
                    template.AppendLine($"\t WHERE   {tableName}.isDelete != 1");
                }
                else
                {
                    template.AppendLine("\t WHERE 1 ");
                }

                // we create a list which field  manually so end user can choose we give em all filter 
                StringBuilder templateSearch = new();
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    if (key.Equals("MUL"))
                    {
                        var foreignTableName = GetForeignKeyTableName(tableName, field);
                        if (foreignTableName == null) continue;
                        var describeTableForeignModels = GetTableStructure(foreignTableName);
                        foreach (var describeTableForeignModel in describeTableForeignModels)
                        {
                            var foreignModelKey = string.Empty;

                            if (describeTableForeignModel.KeyValue != null)
                            {
                                foreignModelKey = describeTableForeignModel.KeyValue;
                            }

                            if (!(foreignModelKey.Equals("PRI") && foreignModelKey.Equals("MULL")))
                            {
                                templateSearch.Append("\t " + foreignTableName + "." +
                                                      GetLabelForComboBoxForGridOrOption(tableName, field) +
                                                      " LIKE CONCAT('%',@search,'%') OR");
                            }
                        }
                    }
                    else
                    {
                        if (!key.Equals("PRI"))
                        {
                            templateSearch.Append("\n\t " + tableName + "." + field +
                                                  " LIKE CONCAT('%',@search,'%') OR");
                        }
                    }
                }

                template.AppendLine("\t AND (" + templateSearch.ToString().TrimEnd('O', 'R') + ")\";");

                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                parameterModels = new List<ParameterModel>");
                template.AppendLine("                {");
                template.AppendLine("                    new ()");
                template.AppendLine("                    {");
                template.AppendLine("                        Key = \"@search\",");
                template.AppendLine("                        Value = search");
                template.AppendLine("                    }");
                template.AppendLine("                };");
                template.AppendLine("                foreach (var parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine(
                    "                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    while (reader.Read())");
                template.AppendLine("                   {");
                template.AppendLine(
                    "                        " + lcTableName + "Models.Add(new " + ucTableName + "Model");
                template.AppendLine("                       {");


                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            if (key.Equals("PRI") || key.Equals("MUL"))
                            {
                                if (!readOnly || !key.Equals("MUL")) continue;
                                // weird later
                                //   if (readOnly && key.Equals("MUL"))
                                //   {
                                template.AppendLine(" " +
                                                    UpperCaseFirst(
                                                        GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                    " = reader[\"" +
                                                    LowerCaseFirst(
                                                        GetLabelForComboBoxForGridOrOption(tableName, field)) +
                                                    "\"].ToString(),");
                                //  }

                                template.AppendLine("                            " +
                                                    UpperCaseFirst(field.Replace("Id", "Key") +
                                                                   " = Convert.ToInt32(reader[\"" +
                                                                   LowerCaseFirst(field)) + "\"]),");
                            }
                            else
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = Convert.ToInt32(reader[\"" + LowerCaseFirst(field) + "\"]),");
                            }
                        }
                        else if (GetDoubleDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = Convert.ToDouble(reader[\"" + LowerCaseFirst(field) + "\"]),");
                        }
                        else if (type.Contains("decimal"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = Convert.ToDecimal(reader[\"" + LowerCaseFirst(field) + "\"]),");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = Convert.ToDateTime(reader[\"" + LowerCaseFirst(field) +
                                                    "\"]),");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = Convert.ToInt32(reader[\"" + LowerCaseFirst(field) + "\"]),");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = (reader[\"" + LowerCaseFirst(field) +
                                                    "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToTime((DateTime)reader[\"" +
                                                    LowerCaseFirst(field) + "\"]): null,");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = (reader[\"" + LowerCaseFirst(field) +
                                                    "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" +
                                                    LowerCaseFirst(field) + "\"]): null,");
                            }
                        }
                        else if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = (byte[])reader[\"" + LowerCaseFirst(field) + "\"],");
                        }
                        else
                        {
                            template.AppendLine("                            " + UpperCaseFirst(field) +
                                                " = reader[\"" + LowerCaseFirst(field) + "\"].ToString(),");
                        }
                    }
                }

                template.AppendLine("});");

                template.AppendLine("                    }");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine(
                    "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");

                template.AppendLine("            return " + lcTableName + "Models;");
                template.AppendLine("        }");

                // View One Record aka findViewById 

                template.AppendLine("        public " + ucTableName + "Model  GetSingle(" + ucTableName + "Model " +
                                    lcTableName + "Model)");
                template.AppendLine("        {");

                template.AppendLine("            var sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                // the main problem is are we should filter em all or else ? 
                template.AppendLine("                connection.Open();");
                template.AppendLine("                sql += @\"");
                template.AppendLine("                SELECT  *");
                template.AppendLine("                FROM    " + tableName + " ");

                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }


                    if (GetHiddenField().Any(x => field.Contains(x)) ||
                        !GetNumberDataType().Any(x => type.Contains(x)) || !key.Equals("MUL")) continue;
                    template.AppendLine("\t JOIN " + GetForeignKeyTableName(tableName, field) + " ");
                    template.AppendLine("\t USING(" + field + ")");
                }

                // how many table join is related ?
                if (isDeleteFieldExisted)
                {
                    template.AppendLine($"                WHERE   {tableName}.isDelete != 1");
                }
                else
                {
                    template.AppendLine(" WHERE 1");
                }

                template.AppendLine("                AND   " + tableName + "." + primaryKey + "    =   @" + primaryKey +
                                    " LIMIT 1\";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                parameterModels = new List<ParameterModel>");
                template.AppendLine("                {");
                template.AppendLine("                    new ()");
                template.AppendLine("                    {");
                template.AppendLine("                        Key = \"@" + primaryKey + "\",");
                if (primaryKey != null)
                {
                    template.AppendLine("                        Value = " + lcTableName + "Model." +
                                        UpperCaseFirst(primaryKey.Replace("Id", "Key")));
                }

                template.AppendLine("                   }");
                template.AppendLine("                };");

                template.AppendLine("                foreach (var parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine(
                    "                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    while (reader.Read())");
                template.AppendLine("                   {");

                // we cannot using  Model.field = "value" as using init in the model 

                template.AppendLine(lcTableName + "Model = new " + ucTableName + "Model() { ");
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    var type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (GetHiddenField().Any(x => field.Contains(x))) continue;
                    {
                        if (GetNumberDataType().Any(x => type.Contains(x)))
                        {
                            switch (key)
                            {
                                case "PRI":
                                case "MUL":
                                    template.AppendLine(UpperCaseFirst(field.Replace("Id", "Key") +
                                                                       " = Convert.ToInt32(reader[\"" +
                                                                       LowerCaseFirst(field)) + "\"]),");
                                    break;
                                default:
                                    template.AppendLine(UpperCaseFirst(field) + " = Convert.ToInt32(reader[\"" +
                                                        LowerCaseFirst(field) + "\"]),");
                                    break;
                            }
                        }
                        else if (GetDoubleDataType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDouble(reader[\"" +
                                                LowerCaseFirst(field) + "\"]),");
                        }
                        else if (type.Contains("decimal"))
                        {
                            template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDecimal(reader[\"" +
                                                LowerCaseFirst(field) + "\"]),");
                        }
                        else if (GetDateDataType().Any(x => type.Contains(x)))
                        {
                            if (type.Contains("datetime"))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDateTime(reader[\"" +
                                                    LowerCaseFirst(field) + "\"]),");
                            }
                            else if (type.Contains("year"))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = Convert.ToInt32(reader[\"" +
                                                    LowerCaseFirst(field) + "\"]),");
                            }
                            else if (type.Contains("time"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = (reader[\"" + LowerCaseFirst(field) +
                                                    "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToTime((DateTime)reader[\"" +
                                                    LowerCaseFirst(field) + "\"]): null,");
                            }
                            else if (type.Contains("date"))
                            {
                                template.AppendLine("                            " + UpperCaseFirst(field) +
                                                    " = (reader[\"" + LowerCaseFirst(field) +
                                                    "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" +
                                                    LowerCaseFirst(field) + "\"]): null,");
                            }
                        }
                        else if (GetBlobType().Any(x => type.Contains(x)))
                        {
                            template.AppendLine(UpperCaseFirst(field) + " = (byte[])reader[\"" + LowerCaseFirst(field) +
                                                "\"],");
                        }
                        else
                        {
                            template.AppendLine(UpperCaseFirst(field) + " = reader[\"" + LowerCaseFirst(field) +
                                                "\"].ToString(),");
                        }
                    }
                }

                template.AppendLine("                    };");

                template.AppendLine("                    }");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine(
                    "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");


                // single with detail value-------------------------

                if (!string.IsNullOrEmpty(tableNameDetail))
                {
                    template.AppendLine("        public " + ucTableName + "Model  GetSingleWithDetail(" + ucTableName +
                                        "Model " + lcTableName + "Model)");
                    template.AppendLine("        {");

                    template.AppendLine("            var sql = string.Empty;");
                    template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                    template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                    template.AppendLine("            try");
                    template.AppendLine("            {");
                    // the main problem is are we should filter em all or else ? 
                    template.AppendLine("                connection.Open();");
                    template.AppendLine("                sql += @\"");
                    template.AppendLine("                SELECT  *");
                    template.AppendLine("                FROM    " + tableName + " ");

                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (GetHiddenField().Any(x => field.Contains(x)) ||
                            !GetNumberDataType().Any(x => type.Contains(x)) || !key.Equals("MUL")) continue;
                        template.AppendLine("\t JOIN " + GetForeignKeyTableName(tableName, field) + " ");
                        template.AppendLine("\t USING(" + field + ")");
                    }

                    // how many table join is related ?
                    if (isDeleteFieldExisted)
                    {
                        template.AppendLine($"                WHERE   {tableName}.isDelete != 1");
                    }
                    else
                    {
                        template.AppendLine(" WHERE 1");
                    }

                    template.AppendLine("                AND   " + tableName + "." + primaryKey + "    =   @" +
                                        primaryKey +
                                        " LIMIT 1\";");
                    template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");

                    template.AppendLine("                parameterModels = new List<ParameterModel>");
                    template.AppendLine("                {");
                    template.AppendLine("                    new ()");
                    template.AppendLine("                    {");
                    template.AppendLine("                        Key = \"@" + primaryKey + "\",");
                    if (primaryKey != null)
                    {
                        template.AppendLine("                        Value = " + lcTableName + "Model." +
                                            UpperCaseFirst(primaryKey.Replace("Id", "Key")));
                    }

                    template.AppendLine("                   }");
                    template.AppendLine("                };");

                    template.AppendLine("                foreach (var parameter in parameterModels)");
                    template.AppendLine("                {");
                    template.AppendLine(
                        "                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                    template.AppendLine("                }");
                    template.AppendLine("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
                    template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                    template.AppendLine("                {");
                    template.AppendLine("                    while (reader.Read())");
                    template.AppendLine("                   {");

                    template.AppendLine(lcTableName + "Model = new " + ucTableName + "Model() { ");
                    foreach (var describeTableModel in describeTableModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (GetHiddenField().Any(x => field.Contains(x))) continue;
                        {
                            if (GetNumberDataType().Any(x => type.Contains(x)))
                            {
                                switch (key)
                                {
                                    case "PRI":
                                    case "MUL":
                                        template.AppendLine(UpperCaseFirst(field.Replace("Id", "Key") +
                                                                           " = Convert.ToInt32(reader[\"" +
                                                                           LowerCaseFirst(field)) + "\"]),");
                                        break;
                                    default:
                                        template.AppendLine(UpperCaseFirst(field) + " = Convert.ToInt32(reader[\"" +
                                                            LowerCaseFirst(field) + "\"]),");
                                        break;
                                }
                            }
                            else if (GetDoubleDataType().Any(x => type.Contains(x)))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDouble(reader[\"" +
                                                    LowerCaseFirst(field) + "\"]),");
                            }
                            else if (type.Contains("decimal"))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDecimal(reader[\"" +
                                                    LowerCaseFirst(field) + "\"]),");
                            }
                            else if (GetDateDataType().Any(x => type.Contains(x)))
                            {
                                if (type.Contains("datetime"))
                                {
                                    template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDateTime(reader[\"" +
                                                        LowerCaseFirst(field) + "\"]),");
                                }
                                else if (type.Contains("year"))
                                {
                                    template.AppendLine(UpperCaseFirst(field) + " = Convert.ToInt32(reader[\"" +
                                                        LowerCaseFirst(field) + "\"]),");
                                }
                                else if (type.Contains("time"))
                                {
                                    template.AppendLine("                            " + UpperCaseFirst(field) +
                                                        " = (reader[\"" + LowerCaseFirst(field) +
                                                        "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToTime((DateTime)reader[\"" +
                                                        LowerCaseFirst(field) + "\"]): null,");
                                }
                                else if (type.Contains("date"))
                                {
                                    template.AppendLine("                            " + UpperCaseFirst(field) +
                                                        " = (reader[\"" + LowerCaseFirst(field) +
                                                        "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" +
                                                        LowerCaseFirst(field) + "\"]): null,");
                                }
                            }
                            else if (GetBlobType().Any(x => type.Contains(x)))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = (byte[])reader[\"" +
                                                    LowerCaseFirst(field) +
                                                    "\"],");
                            }
                            else
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = reader[\"" + LowerCaseFirst(field) +
                                                    "\"].ToString(),");
                            }
                        }
                    }

                    template.AppendLine("                    };");

                    template.AppendLine("                    }");
                    template.AppendLine("                }");
                    template.AppendLine("                mySqlCommand.Dispose();");
                    template.AppendLine("            }");
                    template.AppendLine("            catch (MySqlException ex)");
                    template.AppendLine("            {");
                    template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                    template.AppendLine(
                        "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                    template.AppendLine("                throw new Exception(ex.Message);");
                    template.AppendLine("            }");
                }

                if (!string.IsNullOrEmpty(tableNameDetail))
                {
                    template.AppendLine("            List<" + ucTableNameDetail + "Model> " + lcTableNameDetail +
                                        "Models = new();");

                    template.AppendLine("            try");
                    template.AppendLine("            {");
                    template.AppendLine("                sql = @\"");
                    template.AppendLine("                SELECT      *");
                    template.AppendLine("                FROM        " + tableNameDetail + " ");

                    foreach (var describeTableModel in describeTableDetailModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (GetHiddenField().Any(x => field.Contains(x))) continue;
                        {
                            if (!GetNumberDataType().Any(x => type.Contains(x)) || !key.Equals("MUL")) continue;
                            template.AppendLine("\t JOIN " + GetForeignKeyTableName(tableNameDetail, field) +
                                                " ");
                            template.AppendLine("\t USING(" + field + ")");
                        }
                    }

                    if (isDeleteFieldExisted)
                    {
                        template.AppendLine($"\t WHERE   {tableName}.isDelete != 1 AND {tableNameDetail} != 1");
                    }
                    else
                    {
                        template.AppendLine(" WHERE 1 ");
                    }

                    template.AppendLine("                AND   " + tableNameDetail + "." + primaryKey + "    =   @" +
                                        primaryKey + " \";");


                    template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                    template.AppendLine("                foreach (var parameter in parameterModels)");
                    template.AppendLine("                {");
                    template.AppendLine(
                        "                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                    template.AppendLine("                }");
                    template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                    template.AppendLine("                {");
                    template.AppendLine("                    while (reader.Read())");
                    template.AppendLine("                   {");

                    // we cannot using  Model.field = "value" as using init in the model 

                    template.AppendLine("                        " + lcTableNameDetail + "Models.Add(new " +
                                        ucTableNameDetail + "Model(){");
                    foreach (var describeTableModel in describeTableDetailModels)
                    {
                        var key = string.Empty;
                        var field = string.Empty;
                        var type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                        {
                            key = describeTableModel.KeyValue;
                        }

                        if (describeTableModel.FieldValue != null)
                        {
                            field = describeTableModel.FieldValue;
                        }

                        if (describeTableModel.TypeValue != null)
                        {
                            type = describeTableModel.TypeValue;
                        }

                        if (GetHiddenField().Any(x => field.Contains(x))) continue;
                        {
                            if (GetNumberDataType().Any(x => type.Contains(x)))
                            {
                                switch (key)
                                {
                                    case "PRI":
                                    case "MUL":
                                        template.AppendLine(UpperCaseFirst(field.Replace("Id", "Key") +
                                                                           " = Convert.ToInt32(reader[\"" +
                                                                           LowerCaseFirst(field)) + "\"]),");
                                        break;
                                    default:
                                        template.AppendLine(UpperCaseFirst(field) + " = Convert.ToInt32(reader[\"" +
                                                            LowerCaseFirst(field) + "\"]),");
                                        break;
                                }
                            }
                            else if (GetDoubleDataType().Any(x => type.Contains(x)))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDouble(reader[\"" +
                                                    LowerCaseFirst(field) + "\"]),");
                            }
                            else if (type.Contains("decimal"))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDecimal(reader[\"" +
                                                    LowerCaseFirst(field) + "\"]),");
                            }
                            else if (GetDateDataType().Any(x => type.Contains(x)))
                            {
                                if (type.Contains("datetime"))
                                {
                                    template.AppendLine(UpperCaseFirst(field) + " = Convert.ToDateTime(reader[\"" +
                                                        LowerCaseFirst(field) + "\"]),");
                                }
                                else if (type.Contains("year"))
                                {
                                    template.AppendLine(UpperCaseFirst(field) + " = Convert.ToInt32(reader[\"" +
                                                        LowerCaseFirst(field) + "\"]),");
                                }
                                else if (type.Contains("time"))
                                {
                                    template.AppendLine("                            " + UpperCaseFirst(field) +
                                                        " = (reader[\"" + LowerCaseFirst(field) +
                                                        "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToTime((DateTime)reader[\"" +
                                                        LowerCaseFirst(field) + "\"]): null,");
                                }
                                else if (type.Contains("date"))
                                {
                                    template.AppendLine("                            " + UpperCaseFirst(field) +
                                                        " = (reader[\"" + LowerCaseFirst(field) +
                                                        "\"] != DBNull.Value) ?CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" +
                                                        LowerCaseFirst(field) + "\"]): null,");
                                }
                            }
                            else if (GetBlobType().Any(x => type.Contains(x)))
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = (byte[])reader[\"" +
                                                    LowerCaseFirst(field) + "\"],");
                            }
                            else
                            {
                                template.AppendLine(UpperCaseFirst(field) + " = reader[\"" + LowerCaseFirst(field) +
                                                    "\"].ToString(),");
                            }
                        }
                    }

                    template.AppendLine("                    });");
                    template.AppendLine("                }");
                    template.AppendLine("                }");
                    template.AppendLine("                mySqlCommand.Dispose();");
                    template.AppendLine("            }");
                    template.AppendLine("            catch (MySqlException ex)");
                    template.AppendLine("            {");
                    template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                    template.AppendLine(
                        "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                    template.AppendLine("                throw new Exception(ex.Message);");
                    template.AppendLine("            }");


                    template.AppendLine("if(" + lcTableNameDetail + "Models != null){");
                    template.AppendLine(lcTableName + "Model.Data = " + lcTableNameDetail + "Models;");
                    template.AppendLine("}");
                }


                template.AppendLine("            return " + lcTableName + "Model;");
                template.AppendLine("        }");

                // excel method/function

                template.AppendLine("        public byte[] GetExcel()");
                template.AppendLine("        {");
                template.AppendLine("            using var workbook = new XLWorkbook();");
                template.AppendLine("            var worksheet = workbook.Worksheets.Add(\"Administrator > " +
                                    ucTableName + " \");");
                // loop here

                int dd = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                continue;
                            case "MUL":
                                var fieldName = GetLabelForComboBoxForGridOrOption(tableName, field);
                                template.AppendLine("\t\tworksheet.Cell(1, " + dd + ").Value = \"" +
                                                    SplitToSpaceLabel(fieldName) +
                                                    "\";");
                                dd++;
                                break;
                            default:
                                template.AppendLine("\t\tworksheet.Cell(1, " + dd + ").Value = \"" +
                                                    SplitToSpaceLabel(field.Replace(tableName, "")) +
                                                    "\";");
                                dd++;
                                break;
                        }
                    }
                }
                // loop end

                template.AppendLine("            var sql = _sharedUtil.GetSqlSession();");
                template.AppendLine("           var parameterModels = _sharedUtil.GetListSqlParameter();");
                template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("               connection.Open();");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                if (parameterModels != null)");
                template.AppendLine("                {");
                template.AppendLine("                    foreach (var parameter in parameterModels)");
                template.AppendLine("                    {");
                template.AppendLine(
                    "                        mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                    }");
                template.AppendLine("                }");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    var counter = 3;");
                template.AppendLine("                   while (reader.Read())");
                template.AppendLine("                    {");
                template.AppendLine("                        var currentRow = counter++;");
                // loop here
                var ee = 1;
                foreach (var describeTableModel in describeTableModels)
                {
                    var key = string.Empty;
                    var field = string.Empty;
                    if (describeTableModel.KeyValue != null)
                    {
                        key = describeTableModel.KeyValue;
                    }

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (!GetHiddenField().Any(x => field.Contains(x)))
                    {
                        switch (key)
                        {
                            case "PRI":
                                // do nothing  here 
                                break;
                            case "MUL":
                                template.AppendLine(
                                    "                        worksheet.Cell(currentRow, " + ee +
                                    ").Value = reader[\"" +
                                    GetLabelForComboBoxForGridOrOption(tableName, field) + "\"].ToString();");


                                ee++;
                                break;
                            default:
                                template.AppendLine("worksheet.Cell(currentRow, " + ee +
                                                    ").Value = reader[\"" +
                                                    field +
                                                    "\"].ToString();");
                                ee++;
                                break;
                        }
                    }
                }

                // loop end here
                template.AppendLine("                    }");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");
                template.AppendLine("            using var stream = new MemoryStream();");
                template.AppendLine("           workbook.SaveAs(stream);");
                template.AppendLine("            return stream.ToArray();");
                template.AppendLine("        }");
                template.AppendLine("        public void Update(" + ucTableName + "Model " + lcTableName + "Model)");
                template.AppendLine("        {");

                template.AppendLine("            var sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine(
                    "                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                template.AppendLine("                sql = @\"");
                template.AppendLine("                UPDATE  " + tableName + " ");
                // start loop
                template.AppendLine("                SET     ");
                StringBuilder updateString = new();
                var total = fieldNameList.Count;
                for (var i = 0; i < total; i++)
                {
                    if (fieldNameList[i] == primaryKey) continue;
                    if (i + 1 == fieldNameList.Count)
                    {
                        updateString.AppendLine(fieldNameList[i] + "=@" + fieldNameList[i]);
                    }
                    else
                    {
                        updateString.AppendLine(fieldNameList[i] + "=@" + fieldNameList[i] + ",");
                    }
                }

                template.AppendLine(updateString.ToString().TrimEnd(','));
                // end loop
                template.AppendLine("                WHERE   " + primaryKey + "    =   @" + primaryKey + "\";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");

                // loop end
                template.AppendLine("                parameterModels = new List<ParameterModel>");

                template.AppendLine("                {");
                // loop start

                template.AppendLine("                    new ()");
                template.AppendLine("                    {");
                template.AppendLine("                        Key = \"@" + primaryKey + "\",");
                if (primaryKey != null)
                {
                    template.AppendLine("                        Value = " + lcTableName + "Model." +
                                        UpperCaseFirst(primaryKey.Replace("Id", "Key")));
                }

                template.AppendLine("                   },");
                template.AppendLine(loopColumn.ToString().TrimEnd(','));
                // loop end
                template.AppendLine("                };");
                template.AppendLine("                foreach (var parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine(
                    "                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                template.AppendLine("                mySqlTransaction.Commit();");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine(
                    "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");
                template.AppendLine("        }");
                template.AppendLine("        public void Delete(" + ucTableName + "Model " + lcTableName + "Model)");
                template.AppendLine("        {");
                template.AppendLine("            var sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using var connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine(
                    "                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                template.AppendLine("                sql = @\"");
                if (isDeleteFieldExisted)
                {
                    template.AppendLine("                UPDATE  " + tableName + " ");
                    template.AppendLine("                SET     isDelete    =   1");
                }
                else
                {
                    template.AppendLine("                DELETE " + tableName + " ");
                }

                template.AppendLine("                WHERE   " + primaryKey + "    =   @" + primaryKey + "\";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                parameterModels = new List<ParameterModel>");

                template.AppendLine("                {");
                template.AppendLine("                    new ()");
                template.AppendLine("                    {");
                template.AppendLine("                        Key = \"@" + lcTableName + "Id\",");
                if (primaryKey != null)
                {
                    template.AppendLine("                        Value = " + lcTableName + "Model." +
                                        UpperCaseFirst(primaryKey.Replace("Id", "Key")));
                }

                template.AppendLine("                   }");
                template.AppendLine("                };");
                template.AppendLine("                foreach (var parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine(
                    "                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                template.AppendLine("                mySqlTransaction.Commit();");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine(
                    "                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");
                template.AppendLine("        }");
                template.AppendLine("}");

                return template.ToString();
            }

            private string GetLabelOrPlaceHolderForComboBox(string tableName, string fieldName)
            {
                var name = string.Empty;
                var foreignTableName = GetForeignKeyTableName(tableName, fieldName);
                if (foreignTableName == null) return name;
                var structureModel = GetTableStructure(foreignTableName);
                foreach (var describeTableModel in structureModel)
                {
                    var field = string.Empty;
                    var type = string.Empty;

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (!GetStringDataType().Any(x => type.Contains(x))) continue;
                    if (int.Parse(Regex.Match(type, @"\d+").Value) <= 10) continue;
                    name = SplitToSpaceLabel(field);

                    break;
                }

                return name;
            }

            private string GetLabelForComboBoxForGridOrOption(string tableName, string fieldName)
            {
                var name = string.Empty;
                var foreignTableName = GetForeignKeyTableName(tableName, fieldName);
                if (foreignTableName == null) return name;
                var structureModel = GetTableStructure(foreignTableName);
                foreach (var describeTableModel in structureModel)
                {
                    var field = string.Empty;
                    var type = string.Empty;

                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (describeTableModel.TypeValue != null)
                    {
                        type = describeTableModel.TypeValue;
                    }

                    if (!GetStringDataType().Any(x => type.Contains(x)) ||
                        int.Parse(Regex.Match(type, @"\d+").Value) <= 10) continue;
                    name = field;
                    break;
                }

                return name;
            }

            private bool GetIfExistedIsDeleteField(string tableName)
            {
                var check = false;
                var structureModel = GetTableStructure(tableName);
                foreach (var describeTableModel in structureModel)
                {
                    var field = string.Empty;
                    if (describeTableModel.FieldValue != null)
                    {
                        field = describeTableModel.FieldValue;
                    }

                    if (field.Equals("isDelete"))
                    {
                        check = true;
                        break;
                    }
                }

                return check;
            }

            private string? GetPrimaryKeyTableName(string tableName)
            {
                var referencedTableName = string.Empty;

                using var connection = GetConnection();
                connection.Open();

                var sql = $@"
                SELECT  COLUMN_NAME
		        FROM 	information_schema.KEY_COLUMN_USAGE
		        WHERE 	table_schema='{DefaultDatabase}'
		        AND 	TABLE_NAME = '{tableName}'
		        AND     REFERENCED_COLUMN_NAME IS NULL
                AND     CONSTRAINT_NAME ='PRIMARY'
                LIMIT  1 ";
                var command = new MySqlCommand(sql, connection);
                try
                {
                    referencedTableName = command.ExecuteScalar().ToString();
                }
                catch (MySqlException ex)
                {
                    referencedTableName = string.Empty;
                    Debug.WriteLine(ex.Message);
                }

                return referencedTableName;
            }

            private string? GetForeignKeyTableName(string tableName, string field)
            {
                using var connection = GetConnection();
                connection.Open();
                var referencedTableName = string.Empty;
                var sql = $@"		
            SELECT  REFERENCED_TABLE_NAME
		    FROM 	information_schema.KEY_COLUMN_USAGE
		    WHERE 	table_schema='{DefaultDatabase}'
		    AND 	TABLE_NAME = '{tableName}'
		    AND     COLUMN_NAME='{field}'
            LIMIT  1 ";


                MySqlCommand command = new(sql, connection);
                try
                {
                    referencedTableName = command.ExecuteScalar().ToString();
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine(sql);
                    Console.WriteLine("Table name :" + tableName + " Field : " + field);
                    Console.WriteLine("Error " + ex.Message);
                    Console.WriteLine("----------------------");
                }

                return referencedTableName;
            }

            public static string UpperCaseFirst(string? s)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return string.Empty;
                }

                return char.ToUpper(s[0]) + s[1..];
            }

            private static string LowerCaseFirst(string? s)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return string.Empty;
                }

                return char.ToLower(s[0]) + s[1..];
            }

            private static string SplitToSpaceLabel(string s)
            {
                if (s.Contains(' ', StringComparison.CurrentCulture)) return s;
                Regex r = new(@"
                                (?<=[A-Z])(?=[A-Z][a-z]) |
                                (?<=[^A-Z])(?=[A-Z]) |
                                (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
                var t = r.Replace(s, " ").Replace("_", "").Trim().ToLower();
                var splitTableName = t.Split(' ');
                for (var i = 0; i < splitTableName.Length; i++) // Loop with for.
                {
                    splitTableName[i] = UpperCaseFirst(splitTableName[i]);
                }

                s = string.Join(" ", splitTableName);

                return s;
            }

            public static string SplitToUnderScore(string s)
            {
                Regex r = new(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

                return r.Replace(s, "_").ToLower();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="t"></param>
            /// <param name="type">1 - uppercase first , 0 - lowercase</param>
            /// <returns></returns>
            public static string GetStringNoUnderScore(string t, int type)
            {
                t = t.ToLower();
                if (t.IndexOf("_") > 0)
                {
                    var splitTableName = t.Split('_');
                    for (var i = 0; i < splitTableName.Length; i++) // Loop with for.
                    {
                        if (i > 0)
                        {
                            splitTableName[i] = UpperCaseFirst(splitTableName[i]);
                        }
                    }

                    t = string.Join("", splitTableName);
                }

                if (type == 1)
                {
                    t = UpperCaseFirst(t);
                }

                return t;
            }
        }
    }
}