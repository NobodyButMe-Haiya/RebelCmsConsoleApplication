using System;
namespace RebelCmsConsoleApplication
{
    using MySql.Data.MySqlClient;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Diagnostics;
    namespace RebelCmsGenerator
    {
        internal class CodeGenerator
        {
            private const string DEFAULT_DATABASE = "rebelcms";
            private readonly string connection;
            public enum TextCase
            {
                LcWords,
                UcWords
            }
            public class DatabaseMapping
            {
                public string? TableName { get; set; }
            }
            public class DescribeTableModel
            {
                public string? KeyValue { get; init; }
                public string? FieldValue { get; init; }
                public string? TypeValue { get; init; }
                public string? NullValue { get; init; }
                public string? ExtraValue { get; init; }
            }
            public CodeGenerator(string connection1)
            {
                connection = connection1;

            }
            MySqlConnection GetConnection()
            {
                return new MySqlConnection(connection);
            }
            public static List<string> GetStringDataType()
            {
                return new List<string> { "char", "varchar", "text", "tinytext", "mediumtext", "longtext" };
            }
            public static List<string> GetBlobType()
            {
                return new List<string> { "tinyblob", "mediumblob", "blob", "longblob" };
            }
            public static List<string> GetNumberDataType()
            {
                return new List<string> { "tinyinit", "bool", "boolean", "smallint", "int", "integer", "year", "INT", "YEAR", "SMALLINT", "BOOL", "BOOLEAN" };
            }
            public static List<string> GetNumberDotDataType()
            {
                return new List<string> { "decimal", "float", "double" };
            }
            public static List<string> GetDateDataType()
            {
                return new List<string> { "date", "datetime", "timestamp", "time" };
            }
            public static List<string> GetHiddenField()
            {
                return new List<string> { "tenantId", "isDelete", "executeBy" };
            }
            public static List<string> GetDateFormatUsa()
            {
                return new List<string> {
                "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                     "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                     "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                     "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                     "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
            }
            public static List<string> GetDateFormatNonUsa()
            {
                return new List<string> {
                "M/d/yyyy h:mm:ss tt", "M/d/yyyy h:mm tt",
                     "MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                     "M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                     "M/d/yyyy h:mm", "M/d/yyyy h:mm",
                     "MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
            }
            public void SetByPassMariaDbError()
            {

                using MySqlConnection connection = GetConnection();
                connection.Open();
                try
                {
                    var command = new MySqlCommand("SET character_set_results=utf8", connection);
                    command.ExecuteNonQuery();
                    command.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

            }
            public List<string> GetTableList()
            {
                using MySqlConnection connection = GetConnection();
                connection.Open();
                List<string> tableNames = new();
                string sql = $@"
            SELECT  TABLE_NAME 
            FROM    information_schema.tables 
            WHERE   table_Schema='{DEFAULT_DATABASE}'";
                var command = new MySqlCommand(sql, connection);
                try
                {
                    var reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            if (reader["TABLE_NAME"] != null)
                            {
                                string? name = reader["TABLE_NAME"].ToString();
                                if (name != null)
                                {
                                    tableNames.Add(name);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("No Record");

                    }
                    reader.Close();
                }
                catch (MySqlException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                return tableNames;
            }
            public List<DescribeTableModel> GetTableStructure(string tableName)
            {
                using MySqlConnection connection = GetConnection();
                connection.Open();

                List<DescribeTableModel> describeTableModels = new();
                string sql = $@"DESCRIBE  `{tableName}` ";
                var command = new MySqlCommand(sql, connection);
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
                                TypeValue = reader["Type"].ToString(),
                                NullValue = reader["Null"].ToString(),
                                ExtraValue = reader["Extra"].ToString()
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
            /// This might be some page preview data and mostly this era prefer using data tables with ajax respond 
            /// </summary>
            /// <param name="tableName"></param>
            /// <param name="module"></param>
            /// <returns></returns>
            public string GeneratePagesView(string tableName, string module)
            {
                StringBuilder template = new();

                return template.ToString();
            }
            public string GenerateModel(string tableName, string module)
            {
                var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
                List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
                StringBuilder template = new();
                template.AppendLine($"namespace RebelCmsTemplate.Models.{UpperCaseFirst(module)};");
                template.AppendLine("public class " + GetStringNoUnderScore(tableName, (int)TextCase.UcWords) + "Model");
                template.AppendLine("{");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (Key.Equals("PRI") || Key.Equals("MUL"))
                    {
                        if (Field != null)
                            template.AppendLine("\tpublic int " + UpperCaseFirst(Field.Replace("Id", "Key")) + " { get; init; } ");
                    }
                    else
                    {


                        if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("\tpublic int " + UpperCaseFirst(Field) + " { get; init; } ");
                        }
                        else if (Type.Contains("decimal"))
                        {
                            template.AppendLine("\tpublic decimal " + UpperCaseFirst(Field) + " { get; init; } ");
                        }
                        else if (Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("\tpublic double " + UpperCaseFirst(Field) + " { get; init; } ");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.Equals("datetime"))
                            {
                                template.AppendLine("\tpublic DateTime " + UpperCaseFirst(Field) + "  { get; init; } ");
                            }
                            else if (Type.Equals("date"))
                            {
                                template.AppendLine("\tpublic DateOnly " + UpperCaseFirst(Field) + " { get; init; } ");
                            }
                            else if (Type.Equals("time"))
                            {
                                template.AppendLine("\tpublic TimeOnly " + UpperCaseFirst(Field) + " { get; init; } ");
                            }
                            else if (Type.Equals("year"))
                            {
                                template.AppendLine("\tpublic int " + UpperCaseFirst(Field) + " { get; init; } ");
                            }
                            else
                            {
                                template.AppendLine("\tpublic string? " + UpperCaseFirst(Field) + " { get; init; } ");
                            }
                        }
                        else if (GetBlobType().Any(x => Type.Contains(x)))
                        {
                            // there is a bit issue here . When we received image we convert to byte   while receiving forcing 
                            // byte to convert to base 64 is memory process. JS not perfect. So in the controller we set value to null . not using init for this   
                            // we don't want large field 
                            template.AppendLine("\tpublic byte[]?  " + UpperCaseFirst(Field) + " { get; set; } ");
                            template.AppendLine("\tpublic string?  " + UpperCaseFirst(Field) + "Base64String { get; set; } ");
                        }
                        else
                        {
                            template.AppendLine("\tpublic string? " + UpperCaseFirst(Field) + " { get; init; } ");
                        }

                    }
                }


                template.AppendLine("}");

                return template.ToString();
            }
            public string GenerateController(string tableName, string module, string tableNameMaster = "")
            {
                var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
                List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
                List<string?> fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();

                StringBuilder template = new();
                StringBuilder createModelString = new();
                StringBuilder updateModelString = new();
                int counter = 1;
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;
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
                                createModelString.AppendLine($"\t\t\t{UpperCaseFirst(name)} = {LowerCaseFirst(name)},");

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
                template.AppendLine("using System.Globalization;");
                template.AppendLine($"namespace RebelCmsTemplate.Controllers.Api.{module};");
                template.AppendLine($"[Route(\"api/{module.ToLower()}/[controller]\")]");
                template.AppendLine("[ApiController]");
                template.AppendLine("public class " + ucTableName + "Controller : Controller {");

                template.AppendLine(" private readonly IHttpContextAccessor _httpContextAccessor;");
                template.AppendLine(" private readonly RenderViewToStringUtil _renderViewToStringUtil;");
                template.AppendLine(" public " + ucTableName + "Controller(RenderViewToStringUtil renderViewToStringUtil, IHttpContextAccessor httpContextAccessor)");
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
                template.AppendLine("    var page = await _renderViewToStringUtil.RenderViewToStringAsync(ControllerContext, templatePath);");
                template.AppendLine("    return Ok(page);");
                template.AppendLine("   }");
                template.AppendLine($"   {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"   var content = {lcTableName}Repository.GetExcel();");

                Random random = new();
                var fileName = lcTableName + random.Next(1, 100);

                template.AppendLine($"   return File(content,\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\",\"{fileName}.xlsx\");");
                template.AppendLine("  }");
                template.AppendLine("  [HttpPost]");
                // if got one thing upload image need to put idiot async /
                // this is for single upload . not mutiple . 
                var imageUpload = false;
                var imageFileName = string.Empty;

                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {

                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (GetBlobType().Any(x => Type.Contains(x)))
                    {
                        imageUpload = true;
                        imageFileName = UpperCaseFirst(Field);
                        break;
                    }
                }
                if (!imageUpload)
                {
                    template.AppendLine("  public ActionResult Post()");

                }
                else
                {
                    template.AppendLine("  public async Task<ActionResult> Post()");
                }

                template.AppendLine("  {");
                template.AppendLine("\tvar status = false;");
                template.AppendLine("\tvar mode = Request.Form[\"mode\"];");
                template.AppendLine("\tvar leafCheckKey = Convert.ToInt32(Request.Form[\"leafCheckKey\"]);");
                template.AppendLine($"           {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine("            SharedUtil sharedUtil = new(_httpContextAccessor);");
                template.AppendLine("            CheckAccessUtil checkAccessUtil = new (_httpContextAccessor);");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;



                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Field.Contains("Id"))
                            Field = Field.Replace("Id", "Key");

                        if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine($"\tint {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToInt32(Request.Form[\"{Field}\"]):0;");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {

                                template.AppendLine($"\tDateTime {Field} = DateTime.MinValue;");
                                template.AppendLine($"\tif (!string.IsNullOrEmpty(Request.Form[\"{Field}\"]))");
                                template.AppendLine("\t{");
                                template.AppendLine($"\tvar test = Request.Form[\"{Field}\"].ToString().Split(\"T\");");
                                template.AppendLine("\tvar dateString = test[0].Split(\"-\");");
                                template.AppendLine("\tvar timeString = test[1].Split(\":\");");

                                template.AppendLine("\tvar year = Convert.ToInt32(dateString[0]);");
                                template.AppendLine("\tvar month = Convert.ToInt32(dateString[1]);");
                                template.AppendLine("\tvar day = Convert.ToInt32(dateString[2]);");

                                template.AppendLine("\tvar hour = Convert.ToInt32(timeString[0].ToString());");
                                template.AppendLine("\tvar minute = Convert.ToInt32(timeString[1].ToString());");

                                template.AppendLine($"\t{Field} = new(year, month, day, hour, minute, 0);");
                                template.AppendLine("\t}");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine($"\tDateOnly {Field} = DateOnly.FromDateTime(DateTime.Now);");
                                template.AppendLine($"\tif (!string.IsNullOrEmpty(Request.Form[\"{Field}\"]))");
                                template.AppendLine("\t{");
                                template.AppendLine($"\tvar dateString = Request.Form[\"{Field}\"].ToString().Split(\"-\");");
                                template.AppendLine($"\t{Field} = new DateOnly(Convert.ToInt32(dateString[0]), Convert.ToInt32(dateString[1]), Convert.ToInt32(dateString[2]));");
                                template.AppendLine("\t}");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine($"\tTimeOnly {Field} = TimeOnly.FromDateTime(DateTime.Now);");
                                template.AppendLine($"\tif (!string.IsNullOrEmpty(Request.Form[\"{Field}\"]))");
                                template.AppendLine("\t{");
                                template.AppendLine($"\tvar timeString = Request.Form[\"{Field}\"].ToString().Split(\":\");");
                                template.AppendLine($"\t{Field} = new(Convert.ToInt32(timeString[0].ToString()), Convert.ToInt32(timeString[1].ToString()));");
                                template.AppendLine("\t}");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine($"\tint {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToInt32(Request.Form[\"{Field}\"]):0;");
                            }
                            else
                            {
                                template.AppendLine($"\tint {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToInt32(Request.Form[\"{Field}\"]):0;");
                            }
                        }
                        else if (Type.Contains("double") || Type.Contains("float"))
                        {
                            template.AppendLine($"\tdouble {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToDouble(Request.Form[\"{Field}\"]):0;");

                        }
                        else if (Type.Contains("decimal"))
                        {
                            template.AppendLine($"\tdecimal {Field} =  !string.IsNullOrEmpty(Request.Form[\"{Field}\"])?Convert.ToDecimal(Request.Form[\"{Field}\"]):0;");

                        }
                        else if (GetBlobType().Any(x => Type.Contains(x)))
                        {
                            // a bit long just for checking
                            template.AppendLine($"byte[] {Field} = new byte[100];");
                            template.AppendLine($"foreach (var formFile in Request.Form.Files)");
                            template.AppendLine("{");
                            template.AppendLine($"if (formFile.Name.Equals(\"{Field}\"))");
                            template.AppendLine("{");
                            template.AppendLine($"    {Field} = await sharedUtil.GetByteArrayFromImageAsync(formFile);");
                            template.AppendLine("}");
                            template.AppendLine("}");
                        }
                        else
                        {
                            template.AppendLine($"\tvar {Field} = Request.Form[\"{Field}\"];");
                        }
                    }


                }


                // end loop 
                template.AppendLine("            var search = Request.Form[\"search\"];");


                template.AppendLine($"           List<{ucTableName}Model> data = new();");
                template.AppendLine($"           {ucTableName}Model dataSingle = new();");
                template.AppendLine("            string code;");
                template.AppendLine("            var lastInsertKey = 0;");
                template.AppendLine("            switch (mode)");
                template.AppendLine("            {");
                //create
                template.AppendLine("                case \"create\":");
                template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.CREATE_ACCESS))");
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
                template.AppendLine($"                           lastInsertKey = {lcTableName}Repository.Create({lcTableName}Model);");
                template.AppendLine("                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                //read
                template.AppendLine("                case \"read\":");
                template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"                           data = {lcTableName}Repository.Read();");
                if(imageUpload){
                    //  Return Base 64 String so can easily understand by html/javascript
                    template.AppendLine("data."+imageFileName+"Base64String =sharedUtil.GetImageString(data."+imageFileName+");");
                    // we don't want double data image 
                    template.AppendLine("data."+imageFileName+"=\"\";");
                }
                template.AppendLine("                            code = ((int)ReturnCodeEnum.CREATE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                // search
                template.AppendLine("                case \"search\":");
                template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
                template.AppendLine("                    {");
                template.AppendLine("                        code = ((int)ReturnCodeEnum.ACCESS_DENIED).ToString();");
                template.AppendLine("                    }");
                template.AppendLine("                    else");
                template.AppendLine("                    {");
                template.AppendLine("                        try");
                template.AppendLine("                        {");
                template.AppendLine($"                           data = {lcTableName}Repository.Search(search);");
                template.AppendLine("                            code = ((int)ReturnCodeEnum.READ_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");

                // single record 
                template.AppendLine("                case \"single\":");
                template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.READ_ACCESS))");
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
                template.AppendLine($"                           dataSingle = {lcTableName}Repository.GetSingle({lcTableName}Model);");
                template.AppendLine("                            code = ((int)ReturnCodeEnum.READ_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                // future might single with detail  
                // update
                template.AppendLine("                case \"update\":");
                template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.UPDATE_ACCESS))");
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
                template.AppendLine("                            code = ((int)ReturnCodeEnum.UPDATE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");

                // delete
                template.AppendLine("                case \"delete\":");
                template.AppendLine("                    if (!checkAccessUtil.GetPermission(leafCheckKey, AuthenticationEnum.DELETE_ACCESS))");
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
                template.AppendLine("                            code = ((int)ReturnCodeEnum.DELETE_SUCCESS).ToString();");
                template.AppendLine("                            status = true;");
                template.AppendLine("                        }");
                template.AppendLine("                        catch (Exception ex)");
                template.AppendLine("                        {");
                template.AppendLine("                            code = sharedUtil.GetRoleId() == (int)AccessEnum.ADMINISTRATOR_ACCESS ? ex.Message : ((int)ReturnCodeEnum.SYSTEM_ERROR).ToString();");
                template.AppendLine("                        }");
                template.AppendLine("                    }");
                template.AppendLine("                    break;");
                template.AppendLine("                default:");
                template.AppendLine("                    code = ((int)ReturnCodeEnum.ACCESS_DENIED_NO_MODE).ToString();");
                template.AppendLine("                    break;");
                template.AppendLine("            }");
                template.AppendLine("            if (data.Count > 0)");
                template.AppendLine("            {");
                template.AppendLine("                return Ok(new { status, code, data });");
                template.AppendLine("            }");
                template.AppendLine("            if (mode.Equals(\"single\"))");
                template.AppendLine("            {");
                template.AppendLine("                return Ok(new { status, code, dataSingle });");
                template.AppendLine("            }");
                template.AppendLine("            return lastInsertKey > 0 ? Ok(new { status, code, lastInsertKey }) : Ok(new { status, code });");
                template.AppendLine("        }");
                template.AppendLine("     ");
                template.AppendLine("    }");

                return template.ToString();
            }
            public string GeneratePages(string tableName, string module, string tableNameMaster = "")
            {
                var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
                List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
                List<string?> fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();
                StringBuilder template = new();

                template.AppendLine("@inject Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor");
                template.AppendLine($"@using RebelCmsTemplate.Models.{module}");
                template.AppendLine("@using RebelCmsTemplate.Models.Shared");
                template.AppendLine($"@using RebelCmsTemplate.Repository.{module}");
                template.AppendLine("@using RebelCmsTemplate.Util;");
                template.AppendLine("@using RebelCmsTemplate.Enum;");
                template.AppendLine("@{");
                template.AppendLine("    SharedUtil sharedUtils = new(_httpContextAccessor);");
                template.AppendLine($"    List<{ucTableName}Model> {lcTableName}Models = new();");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        // do nothing here 
                        if (!Field.Equals("tenantId"))
                            template.AppendLine($"    List<{UpperCaseFirst(Field.Replace("Id", ""))}Model> {LowerCaseFirst(Field.Replace("Id", ""))}Models = new();");

                    }
                }
                template.AppendLine("    try");
                template.AppendLine("    {");
                template.AppendLine($"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"       {lcTableName}Models = {lcTableName}Repository.Read();");

                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        if (!Field.Equals("tenantId"))
                        {
                            template.AppendLine($"       {UpperCaseFirst(Field.Replace("Id", ""))}Repository {LowerCaseFirst(Field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                            template.AppendLine($"       {LowerCaseFirst(Field.Replace("Id", ""))}Models = {LowerCaseFirst(Field.Replace("Id", ""))}Repository.Read();");
                        }

                    }
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
                template.AppendLine("                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
                template.AppendLine("                    <ol class=\"breadcrumb\">");
                template.AppendLine("                        <li class=\"breadcrumb-item\">");
                template.AppendLine("                            <a href=\"#\">");
                template.AppendLine("                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
                template.AppendLine("                            </a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-file-excel\"></i>");
                template.AppendLine("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
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
                template.AppendLine("                                <input name=\"search\" id=\"search\" class=\"form-control\"");
                template.AppendLine("                                    placeholder=\"Please Enter Name  Or Other Here\" maxlength=\"64\"");
                template.AppendLine("                                  style =\"width: 350px!important;\" />");
                template.AppendLine("                            </div>");
                template.AppendLine("                        </div>");
                template.AppendLine("                    </div>");
                template.AppendLine("                   <div class=\"card-footer\">");
                template.AppendLine("                        <button type=\"button\" class=\"btn btn-info\" onclick=\"searchRecord()\">");
                template.AppendLine("                            <i class=\"fas fa-filter\"></i> Filter");
                template.AppendLine("                        </button>");
                template.AppendLine("                        &nbsp;");
                template.AppendLine("                        <button type=\"button\" class=\"btn btn-warning\" onclick=\"resetRecord()\">");
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
                template.AppendLine("                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
                template.AppendLine("                            <thead>");
                template.AppendLine("                                <tr>");
                // loop here
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Key.Equals("PRI"))
                        {
                            // do nothing here 

                        }
                        else if (Key.Equals("MUL"))
                        {
                            if (!Field.Equals("tenantId"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <select name=\"{Field.Replace("Id", "Key")}\" id=\"{Field.Replace("Id", "Key")}\" class=\"form-control\">");
                                template.AppendLine($"                                              @if ({Field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("                                                {");
                                template.AppendLine("                                                  <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                                else");
                                template.AppendLine("                                                {");
                                template.AppendLine($"                                                foreach (var row" + UpperCaseFirst(Field.Replace("Id", "")) + " in " + LowerCaseFirst(Field.Replace("Id", "")) + "Models)");
                                template.AppendLine("                                                {");
                                template.AppendLine($"                                                   <option value=\"@row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key\">");
                                var optionLabel = GetLabelOrPlaceHolderForComboBox(tableName, Field);
                                template.AppendLine("                                                   @row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(optionLabel) + "</option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                               }");

                                template.AppendLine("                                             </select>");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                        }
                        else if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" step=\"0.01\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"datetime-local\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"date\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"time\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"number\" type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                        }
                        else
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }

                    }
                }
                // end loop
                template.AppendLine("                                    <td style=\"text-align: center\">");
                template.AppendLine("                                        <Button type=\"button\" class=\"btn btn-info\" onclick=\"createRecord()\">");
                template.AppendLine("                                            <i class=\"fa fa-newspaper\"></i>&nbsp;&nbsp;CREATE");
                template.AppendLine("                                        </Button>");
                template.AppendLine("                                    </td>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                                <tr>");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (!Key.Equals("PRI"))
                        {
                            template.AppendLine("                                    <th>" + Field.Replace(lcTableName, "").Replace("Id", "") + "</th>");
                        }
                    }

                }
                template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                            </thead>");
                template.AppendLine("                            <tbody id=\"tableBody\">");
                template.AppendLine($"                                @foreach (var row in {lcTableName}Models)");
                template.AppendLine("                                {");
                template.AppendLine($"                                    <tr id='{lcTableName}-@row.{ucTableName}Key'>");
                /// loop here 
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Key.Equals("PRI"))
                        {
                            // do nothing here 

                        }
                        else if (Key.Equals("MUL"))
                        {
                            if (!Field.Equals("tenantId"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <select name=\"{Field.Replace("Id", "Key")}\" id=\"{Field.Replace("Id", "Key")}-@row.{ucTableName}Key\" class=\"form-control\">");
                                template.AppendLine($"                                              @if ({Field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("                                                {");
                                template.AppendLine("                                                  <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                                else");
                                template.AppendLine("                                                {");
                                template.AppendLine("                       foreach (var option in from row" + UpperCaseFirst(Field.Replace("Id", "")) + " in " + LowerCaseFirst(Field.Replace("Id", "")) + "Models");
                                template.AppendLine("                        let selected = row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key ==");
                                template.AppendLine("                       row." + UpperCaseFirst(Field.Replace("Id", "")) + "Key");
                                template.AppendLine("                       select selected ? Html.Raw(\"<option value='\" +");
                                template.AppendLine("                       row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key + \"' selected>\" +");
                                var optionLabel = GetLabelOrPlaceHolderForComboBox(tableName, Field);
                                template.AppendLine("                       row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(optionLabel) + " + \"</option>\") :");
                                template.AppendLine("                       Html.Raw(\"<option value=\\\"\" + row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key +");
                                template.AppendLine("                       \"\\\">\" + row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Name + \"</option>\"))");
                                template.AppendLine(" {");
                                template.AppendLine("     @option");
                                template.AppendLine("                                  }");
                                template.AppendLine("                                               }");

                                template.AppendLine("                                             </select>");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }

                        }
                        else if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\"  value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" step=\"0.01\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\"  value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"datetime-local\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"yyyy-MM-ddTHH:mm:ss\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"date\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"yyyy-MM-dd\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"time\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"HH:mm:ss\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"yyyy\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                        }
                        else
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}-@row.{ucTableName}Key\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                    }
                }
                // loop here
                template.AppendLine("                                        <td style=\"text-align: center\">");
                template.AppendLine("                                            <div class=\"btn-group\">");
                template.AppendLine($"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord(@row.{ucTableName}Key)\">");
                template.AppendLine("                                                    <i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                                &nbsp;");
                template.AppendLine($"                                                <Button type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord(@row.{ucTableName}Key)\">");
                template.AppendLine("                                                    <i class=\"fas fa-trash\"></i>&nbsp;DELETE");
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
                template.AppendLine("                    </div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </div>");
                template.AppendLine("        </div>");
                template.AppendLine("    </section>");
                template.AppendLine("    <script>");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    // later custom validator 
                    if (Key.Equals("MUL"))
                    {
                        if (!Field.Equals("tenantId"))
                            template.AppendLine(" var " + Field.Replace("Id", "") + "Models = @Json.Serialize(" + Field.Replace("Id", "") + "Models);");
                    }

                }
                StringBuilder templateField = new();
                StringBuilder oneLineTemplateField = new();
                StringBuilder createTemplateField = new();
                StringBuilder updateTemplateField = new();
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            templateField.Append("row." + name.Replace("Id", "Key") + ",");
                            oneLineTemplateField.Append(name.Replace("Id", "Key") + ",");
                        }
                        else
                        {
                            templateField.Append("row." + name + ",");
                            oneLineTemplateField.Append(name + ",");
                            createTemplateField.Append(name + ".val(),");
                        }
                    }
                };
                template.AppendLine("        function resetRecord() {");
                template.AppendLine("         readRecord();");
                template.AppendLine("         $(\"#search\").val(\"\");");
                template.AppendLine("        }");
                template.AppendLine("        function emptyTemplate() {");
                template.AppendLine("         return\"<tr><td colspan='4'>It's lonely here</td></tr>\";");
                template.AppendLine("        }");
                // remember to one row template here as function name 
                template.AppendLine("        function template(" + oneLineTemplateField.ToString().TrimEnd(',') + ") {");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        // do nothing here 
                        if (!Field.Equals("tenantId"))
                        {
                            template.AppendLine("\tlet " + Field.Replace("Id", "Key") + "Options = \"\";");
                            template.AppendLine("\tlet i = 0;");
                            template.AppendLine("\t" + Field.Replace("Id", "") + "Models.map((row) => {");
                            template.AppendLine("\t\ti++;");
                            template.AppendLine("\t\tconst selected = (parseInt(row." + Field.Replace("Id", "Key") + ") === parseInt(" + Field.Replace("Id", "Key") + ")) ? \"selected\" : \"\";");
                            template.AppendLine("\t\t" + Field.Replace("Id", "Key") + "Options += \"<option value='\" + row." + Field.Replace("Id", "Key") + " + \"' \" + selected + \">\" + row." + Field.Replace("Id", "") + "Name +\"</option>\";");
                            template.AppendLine("\t});");
                        }
                    }
                }
                template.AppendLine("            let template =  \"\" +");
                template.AppendLine($"                \"<tr id='{lcTableName}-\" + {lcTableName}Key + \"'>\" +");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            if (!Key.Equals("PRI"))
                            {
                                if (!Field.Equals("tenantId"))
                                {
                                    if (Key.Equals("MUL"))
                                    {
                                        template.AppendLine("\t\t\"<td class='tdNormalAlign'>\" +");
                                        template.AppendLine("\t\t\t\" <label>\" +");
                                        template.AppendLine("\t\t\t\t\"<select id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"' class='form-control'>\";");
                                        template.AppendLine("\t\ttemplate += " + Field.Replace("Id", "Key") + "Options;");
                                        template.AppendLine("\t\ttemplate += \"</select>\" +");
                                        template.AppendLine("\t\t\"</label>\" +");
                                        template.AppendLine("\t\t\"</td>\" +");
                                    }
                                    else
                                    {
                                        template.AppendLine("                                    \"<td>\" +");
                                        template.AppendLine("                                        \"<label>\" +");
                                        template.AppendLine("                                            \"<input type='number' name='" + Field.Replace("Id", "Key") + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                                        template.AppendLine("                                        \"</label>\" +");
                                        template.AppendLine("                                    \"</td>\" +");
                                    }
                                }
                            }
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("                                    \"<td>\" +");
                            template.AppendLine("                                        \"<label>\" +");
                            template.AppendLine("                                            \"<input type='number' step='0.01' name='" + Field + "' id='" + Field + "-\"+" + lcTableName + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                            template.AppendLine("                                        \"</label>\" +");
                            template.AppendLine("                                    \"</td>\" +");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='datetime-local' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='date' name='" + LowerCaseFirst(Field) + "'  id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"' value='\"+" + LowerCaseFirst(Field) + ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='time' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"' value='\"+" + LowerCaseFirst(Field) + ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='number' min='1900' max='2099' step='1' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"' value='\"+" + LowerCaseFirst(Field) + ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='text' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                   \"</td>\" +");
                            }
                        }
                        else
                        {
                            template.AppendLine("                                    \"<td>\" +");
                            template.AppendLine("                                        \"<label>\" +");
                            template.AppendLine($"                                            \"<input type='text' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableName + "Key+\"'' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                            template.AppendLine("                                        \"</label>\" +");
                            template.AppendLine("                                    \"</td>\" +");
                        }
                    }

                }
                template.AppendLine("                \"<td style='text-align: center'><div class='btn-group'>\" +");
                template.AppendLine($"                \"<Button type='button' class='btn btn-warning' onclick='updateRecord(\" + {lcTableName}Key + \")'>\" +");
                template.AppendLine("                \"<i class='fas fa-edit'></i> UPDATE\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"&nbsp;\" +");
                template.AppendLine($"                \"<Button type='button' class='btn btn-danger' onclick='deleteRecord(\" + {lcTableName}Key + \")'>\" +");
                template.AppendLine("                \"<i class='fas fa-trash'></i> DELETE\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"</div></td>\" +");
                template.AppendLine("                \"</tr>\";");
                template.AppendLine("               return template; ");
                template.AppendLine("        }");
                template.AppendLine("        function createRecord() {");
                // loop here 
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;
                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"         const {name.Replace("Id", "Key")} = $(\"#{name.Replace("Id", "Key")}\");");
                        }
                        else
                        {
                            template.AppendLine($"         const {name} = $(\"#{name}\");");
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
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"            {name.Replace("Id", "Key")}: {name.Replace("Id", "Key")}.val(),");
                        }
                        else
                        {
                            template.AppendLine($"            {name}: {name}.val(),");
                        }
                    }
                }
                // loop here
                template.AppendLine("           },statusCode: {");
                template.AppendLine("            500: function () {");
                template.AppendLine("             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("             $(\"#tableBody\").prepend(template(lastInsertKey," + createTemplateField.ToString().TrimEnd(',') + "));");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("               title: 'Success!',");
                template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                template.AppendLine("               icon: 'success',");
                template.AppendLine("               confirmButtonText: 'Cool'");
                template.AppendLine("             });");
                // loop here
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"\t{name.Replace("Id", "Key")}.val('');");
                        }
                        else
                        {
                            template.AppendLine("\t" + name + ".val('');");
                        }
                    }
                }
                // loop here
                template.AppendLine("            } else if (status === false) {");
                template.AppendLine("             if (typeof(code) === 'string'){");
                template.AppendLine("             @{");
                template.AppendLine("              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }else{");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine("            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval;");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("                Swal.showLoading();");
                template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                timerInterval = setInterval(() => {");
                template.AppendLine("                b.textContent = Swal.getTimerLeft()");
                template.AppendLine("               }, 100)");
                template.AppendLine("              },");
                template.AppendLine("              willClose: () => {");
                template.AppendLine("               clearInterval(timerInterval)");
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
                template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("                  row = data.data[i];");
                // remember one line row 
                template.AppendLine("                  templateStringBuilder += template(" + templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                } else {");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("                 row = data.data[i];");
                // remember one line row 

                template.AppendLine("                 templateStringBuilder += template(" + templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                }");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("               }");
                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval) }");
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
                template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("           " + lcTableName + "Key: " + lcTableName + "Key,");
                // loop not primary
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;

                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {

                        if (name.Contains("Id"))
                        {
                            if (name != lcTableName + "Id")
                                template.AppendLine($"           {name.Replace("Id", "Key")}: $(\"#{name.Replace("Id", "Key")}-\" + {lcTableName}Key).val(),");
                        }
                        else
                        {
                            template.AppendLine($"           {name}: $(\"#{name}-\" + {lcTableName}Key).val(),");
                        }
                    }
                }
                // loop here
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success')");
                template.AppendLine("           } else if (status === false) {");
                template.AppendLine("            if (typeof(code) === 'string'){");
                template.AppendLine("            @{");
                template.AppendLine("             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("              else");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine("            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("              Swal.showLoading();");
                template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("               timerInterval = setInterval(() => {");
                template.AppendLine("               b.textContent = Swal.getTimerLeft()");
                template.AppendLine("              }, 100)");
                template.AppendLine("             },");
                template.AppendLine("             willClose: () => {");
                template.AppendLine("              clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");
                template.AppendLine("        function deleteRecord(" + lcTableName + "Key) { ");
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
                template.AppendLine("             " + lcTableName + "Key: " + lcTableName + "Key");
                template.AppendLine("            }, statusCode: {");
                template.AppendLine("             500: function () {");
                template.AppendLine("              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("             }");
                template.AppendLine("            },");
                template.AppendLine("            beforeSend: function () {");
                template.AppendLine("             console.log(\"loading..\");");
                template.AppendLine("           }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                template.AppendLine("               $(\"#" + lcTableName + "-\" + " + lcTableName + "Key).remove();");
                template.AppendLine("               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
            public string GeneratePagesFormAndGrid(string tableName, string module)
            {
                var primaryKey = GetPrimayKeyTableName(tableName);

                int gridMax = 6;
                StringBuilder template = new();

                var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);
                List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
                List<string?> fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();

                template.AppendLine("@inject Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor");
                template.AppendLine($"@using RebelCmsTemplate.Models.{module}");
                template.AppendLine("@using RebelCmsTemplate.Models.Shared");
                template.AppendLine($"@using RebelCmsTemplate.Repository.{module}");
                template.AppendLine("@using RebelCmsTemplate.Util;");
                template.AppendLine("@using RebelCmsTemplate.Enum;");
                template.AppendLine("@{");
                template.AppendLine("    SharedUtil sharedUtils = new(_httpContextAccessor);");
                template.AppendLine($"    List<{ucTableName}Model> {lcTableName}Models = new();");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        // do nothing here 
                        if (!Field.Equals("tenantId"))
                            template.AppendLine($"    List<{UpperCaseFirst(Field.Replace("Id", ""))}Model> {LowerCaseFirst(Field.Replace("Id", ""))}Models = new();");

                    }
                }
                template.AppendLine("    try");
                template.AppendLine("    {");
                template.AppendLine($"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"       {lcTableName}Models = {lcTableName}Repository.Read();");

                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        if (!Field.Equals("tenantId"))
                        {
                            template.AppendLine($"       {UpperCaseFirst(Field.Replace("Id", ""))}Repository {LowerCaseFirst(Field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                            template.AppendLine($"       {LowerCaseFirst(Field.Replace("Id", ""))}Models = {LowerCaseFirst(Field.Replace("Id", ""))}Repository.Read();");
                        }

                    }
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
                template.AppendLine("                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
                template.AppendLine("                    <ol class=\"breadcrumb\">");
                template.AppendLine("                        <li class=\"breadcrumb-item\">");
                template.AppendLine("                            <a href=\"#\">");
                template.AppendLine("                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
                template.AppendLine("                            </a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-file-excel\"></i>");
                template.AppendLine("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
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
                template.AppendLine($"                                                <Button id=\"createButton\" type=\"button\" class=\"btn btn-success\" onclick=\"createRecord()\">");
                template.AppendLine("                                                    <i class=\"fas fa-newspaper\"></i>&nbsp;CREATE");
                template.AppendLine("                                                </Button>&nbsp;");
                template.AppendLine($"                                                <Button id=\"updateButton\" type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord()\" disabled=\"disabled\">");
                template.AppendLine("                                                    <i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
                template.AppendLine("                                                </Button>&nbsp;");

                template.AppendLine($"                                                <Button id=\"deleteButton\" type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord()\" disabled=\"disabled\">");
                template.AppendLine("                                                    <i class=\"fas fa-trash\"></i>&nbsp;DELETE");

                template.AppendLine("                                                </Button>&nbsp;");

                template.AppendLine("                        <button type=\"button\" class=\"btn btn-warning\" onclick=\"resetForm()\">");
                template.AppendLine("                            <i class=\"fas fa-power-off\"></i> Reset");
                template.AppendLine("                        </button>");


                template.AppendLine("                    </div>");
                template.AppendLine("                    <div class=\"card-body\">");
                template.AppendLine("         <div class=\"row\">");
                int d = 0;
                int i = 0;
                int total = describeTableModels.Count;
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Key.Equals("PRI"))
                        {
                            // do nothing here
                            template.AppendLine($"\t<input type=\"hidden\" id=\"{Field}\" />");
                            // don't calculate this for two
                            d--;
                        }
                        else if (Key.Equals("MUL"))
                        {
                            if (!Field.Equals("tenantId"))
                            {
                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("\t<div class=\"form-group\"");
                                template.AppendLine($"\t\t<label for=\"{Field.Replace("Id", "Key")}\">{SplitToSpaceLabel(Field.Replace("Id", ""))}</label>");
                                template.AppendLine($"\t\t<select id=\"{Field.Replace("Id", "Key")}\" class=\"form-control\">");
                                template.AppendLine($"                                            <select name=\"{Field.Replace("Id", "Key")}\" id=\"{Field.Replace("Id", "Key")}\" class=\"form-control\">");
                                template.AppendLine($"                                              @if ({Field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("                                                {");
                                template.AppendLine("                                                  <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                                else");
                                template.AppendLine("                                                {");
                                template.AppendLine($"                                                foreach (var row" + UpperCaseFirst(Field.Replace("Id", "")) + " in " + LowerCaseFirst(Field.Replace("Id", "")) + "Models)");
                                template.AppendLine("                                                {");
                                template.AppendLine($"                                                   <option value=\"@row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key\">");
                                var optionLabel = GetLabelOrPlaceHolderForComboBox(tableName, Field);
                                template.AppendLine("                                                   @row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(optionLabel) + "</option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                               }");

                                template.AppendLine("                                             </select>");

                                template.AppendLine("\t\t</select>");
                                template.AppendLine("\t</div>");
                                template.AppendLine("\t</div>");
                            }

                        }
                        else if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("<div class=\"col-md-6\">");
                            template.AppendLine("<div class=\"form-group\">");
                            template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                            template.AppendLine($"\t<input type=\"number\" id=\"{Field}\" class=\"form-control\" placeholder=\"\" />");
                            template.AppendLine("</div>");
                            template.AppendLine("</div>");
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("<div class=\"col-md-6\">");
                            template.AppendLine("<div class=\"form-group\">");
                            template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                            template.AppendLine($"\t<input type=\"number\" step=\"0.01\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("</div>");
                            template.AppendLine("</div>");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                                template.AppendLine($"\t<input type=\"datetime-local\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                                template.AppendLine("</div>");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                                template.AppendLine($"\t<input type=\"date\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                                template.AppendLine("</div>");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                                template.AppendLine($"\t<input type=\"time\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                                template.AppendLine("</div>");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                                template.AppendLine($"\t<input type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                                template.AppendLine("</div>");
                            }
                            else
                            {
                                template.AppendLine("<div class=\"col-md-6\">");
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                                template.AppendLine($"\t<input type=\"text\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                                template.AppendLine("</div>");
                            }
                        }
                        else if (GetBlobType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("<div class=\"col-md-6\">");
                            template.AppendLine("<div class=\"form-group\">");
                            template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                            template.AppendLine($"\t<input type=\"file\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("</div>");
                            template.AppendLine("</div>");
                        }
                        else
                        {
                            template.AppendLine("<div class=\"col-md-6\">");
                            template.AppendLine("<div class=\"form-group\">");
                            template.AppendLine($"\t<label for=\"{Field}\">{SplitToSpaceLabel(Field.Replace(tableName, ""))}</label>");
                            template.AppendLine($"\t<input type=\"text\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("</div>");
                            template.AppendLine("</div>");
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
                template.AppendLine("\t\t<input name=\"search\" id=\"search\" class=\"form-control\" placeholder=\"Please Enter Name  Or Other Here\" maxlength=\"64\" style =\"width: 350px!important;\" />");
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
                template.AppendLine("                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
                template.AppendLine("                            <thead>");
                template.AppendLine("                            <tr>");
                int h = 1;
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (!Key.Equals("PRI"))
                        {
                            template.AppendLine("                                    <th>" + SplitToSpaceLabel(Field.Replace(lcTableName, "").Replace("Id", "")) + "</th>");
                            i++;
                        }
                        if (h == gridMax)
                        {
                            break;
                        }
                        h++;
                    }

                }
                template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                            </thead>");
                template.AppendLine("                            <tbody id=\"tableBody\">");
                template.AppendLine($"                                @foreach (var row in {lcTableName}Models)");
                template.AppendLine("                                {");
                template.AppendLine($"                                    <tr id='{lcTableName}-@row.{ucTableName}Key'>");
                /// loop here . here should be read only !
                int j = 1;
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Key.Equals("PRI"))
                        {
                            // do nothing here 

                        }
                        else if (Key.Equals("MUL"))
                        {
                            if (!Field.Equals("tenantId"))
                            {
                                template.AppendLine("                                    <td>");
                                var optionLabel = GetLabelOrPlaceHolderForComboBox(tableName, Field);
                                template.AppendLine("@row." + UpperCaseFirst(optionLabel));
                                template.AppendLine("                                    </td>");
                            }

                        }
                        else if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
                            }
                            else
                            {
                                template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
                            }
                        }
                        else
                        {
                            template.AppendLine($"                                    <td>@row.{UpperCaseFirst(Field)}</td>");
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
                template.AppendLine($"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"viewRecord(@row.{ucTableName}Key)\">");
                template.AppendLine("                                                    <i class=\"fas fa-edit\"></i>&nbsp;VIEW");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                       </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine($"                                @if ({lcTableName}Models.Count == 0)");
                template.AppendLine("                                {");
                template.AppendLine("                                    <tr>");
                template.AppendLine("                                        <td colspan=\"" + describeTableModels.Count + "\" class=\"noRecord\">");
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
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    // later custom validator 
                    if (Key.Equals("MUL"))
                    {
                        if (!Field.Equals("tenantId"))
                            template.AppendLine(" var " + Field.Replace("Id", "") + "Models = @Json.Serialize(" + Field.Replace("Id", "") + "Models);");
                    }

                }
                StringBuilder templateField = new();
                StringBuilder oneLineTemplateField = new();
                StringBuilder createTemplateField = new();
                StringBuilder updateTemplateField = new();
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            templateField.Append("row." + name.Replace("Id", "Key") + ",");
                            oneLineTemplateField.Append(name.Replace("Id", "Key") + ",");
                        }
                        else
                        {
                            templateField.Append("row." + name + ",");
                            oneLineTemplateField.Append(name + ",");
                            createTemplateField.Append(name + ".val(),");
                        }
                    }
                };
                template.AppendLine("        function resetForm() {");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;


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
                template.AppendLine("        function resetRecord() {");
                template.AppendLine("         readRecord();");
                template.AppendLine("         $(\"#search\").val(\"\");");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;


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
                template.AppendLine("         return\"<tr><td colspan='" + describeTableModels.Count + "'>It's lonely here</td></tr>\";");
                template.AppendLine("        }");
                // remember to one row template here as function name
                // this only for view purpose only 
                template.AppendLine("        function template(" + oneLineTemplateField.ToString().TrimEnd(',') + ") {");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        // do nothing here  
                        if (!Field.Equals("tenantId"))
                        {
                            template.AppendLine("\tlet " + Field.Replace("Id", "Key") + "Options = \"\";");
                            template.AppendLine("\tlet i = 0;");
                            template.AppendLine("\t" + Field.Replace("Id", "") + "Models.map((row) => {");
                            template.AppendLine("\t\ti++;");
                            template.AppendLine("\t\tconst selected = (parseInt(row." + Field.Replace("Id", "Key") + ") === parseInt(" + Field.Replace("Id", "Key") + ")) ? \"selected\" : \"\";");
                            var optionLabel = GetLabelOrPlaceHolderForComboBox(tableName, Field);
                            template.AppendLine("\t\t" + Field.Replace("Id", "Key") + "Options += \"<option value='\" + row." + Field.Replace("Id", "Key") + " + \"' \" + selected + \">\" + row." + UpperCaseFirst(optionLabel) + " +\"</option>\";");
                            template.AppendLine("\t});");
                        }
                    }
                }
                template.AppendLine("            let template =  \"\" +");
                template.AppendLine($"                \"<tr id='{lcTableName}-\" + {lcTableName}Key + \"'>\" +");
                int m = 1;
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            if (!Key.Equals("PRI"))
                            {
                                if (!Field.Equals("tenantId"))
                                {
                                    if (Key.Equals("MUL"))
                                    {
                                        // this is a a bit hard upon your need to change a lot here ! but better manually 
                                        template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");
                                    }
                                    else
                                    {
                                        template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");

                                    }
                                }
                            }
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");

                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");

                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");

                            }
                            else
                            {
                                template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");

                            }
                        }
                        else
                        {
                            template.AppendLine("                                    \"<td>\"+" + LowerCaseFirst(Field) + "+\"</td>\" +");
                        }
                        if (m == gridMax)
                        {
                            break;
                        }
                        m++;
                    }

                }
                template.AppendLine("                \"<td style='text-align: center'><div class='btn-group'>\" +");
                template.AppendLine($"                \"<Button type='button' class='btn btn-warning' onclick='viewRecord(\" + {lcTableName}Key + \")'>\" +");
                template.AppendLine("                \"<i class='fas fa-search'></i> View\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"</div></td>\" +");
                template.AppendLine("                \"</tr>\";");
                template.AppendLine("               return template; ");
                template.AppendLine("        }");
                template.AppendLine("        function createRecord() {");
                // loop here 
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;
                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"         const {name.Replace("Id", "Key")} = $(\"#{name.Replace("Id", "Key")}\");");
                        }
                        else
                        {
                            template.AppendLine($"         const {name} = $(\"#{name}\");");
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
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"            {name.Replace("Id", "Key")}: {name.Replace("Id", "Key")}.val(),");
                        }
                        else
                        {
                            template.AppendLine($"            {name}: {name}.val(),");
                        }
                    }
                }
                // loop here
                template.AppendLine("           },statusCode: {");
                template.AppendLine("            500: function () {");
                template.AppendLine("             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("             $(\"#tableBody\").prepend(template(lastInsertKey," + createTemplateField.ToString().TrimEnd(',') + "));");
                // put value in input hidden
                // flip create button disabled
                // flip update button enabled
                // flip disabled button enabled
                template.AppendLine("  $(\"" + lcTableName + "Key\").val(lastInsertKey);");
                template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").remoteAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").remoteAttr(\"disabled\",\"disabled\");");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("               title: 'Success!',");
                template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                template.AppendLine("               icon: 'success',");
                template.AppendLine("               confirmButtonText: 'Cool'");
                template.AppendLine("             });");

                template.AppendLine("            } else if (status === false) {");
                template.AppendLine("             if (typeof(code) === 'string'){");
                template.AppendLine("             @{");
                template.AppendLine("              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }else{");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine("            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval;");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("                Swal.showLoading();");
                template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                timerInterval = setInterval(() => {");
                template.AppendLine("                b.textContent = Swal.getTimerLeft()");
                template.AppendLine("               }, 100)");
                template.AppendLine("              },");
                template.AppendLine("              willClose: () => {");
                template.AppendLine("               clearInterval(timerInterval)");
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
                template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("                  row = data.data[i];");
                // remember one line row 
                template.AppendLine("                  templateStringBuilder += template(" + templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                } else {");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("                 row = data.data[i];");
                // remember one line row 

                template.AppendLine("                 templateStringBuilder += template(" + templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                }");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("               }");
                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval) }");
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
                template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                // excel record
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableName + "\");");
                template.AppendLine("        }");
                // view record
                if(primaryKey!= null)
                template.AppendLine("        function viewRecord("+LowerCaseFirst(primaryKey.Replace("Id","Key"))+") {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableName + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"single\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                if(primaryKey!= null)
                template.AppendLine("           " + LowerCaseFirst(primaryKey.Replace("Id","Key")) + ": "+LowerCaseFirst(primaryKey.Replace("Id","Key")));
                template.AppendLine("          }, ");
                template.AppendLine("          statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                //@todo need detail tommorow if  blob need to generated the images 
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                     if (!Key.Equals("PRI"))
                            {
                                if (!Field.Equals("tenantId"))
                                {
                        template.AppendLine("\t$(\"#" + LowerCaseFirst(Field.Replace("Id", "Key")) + "\").val(data.dataSingle." + LowerCaseFirst(Field) + ");");
                                }
                    }            else if (GetBlobType().Any(x => Type.Contains(x)))
                        {
                            // here we implement dummy image preview 
                        template.AppendLine("\t$(\"#" + LowerCaseFirst(Field) + "\").attr(url,data.dataSingle." + LowerCaseFirst(Field) + "64BaseString);");
                        }
                    else
                    {
                        template.AppendLine("\t$(\"#" + LowerCaseFirst(Field) + "\").val(data.dataSingle." + LowerCaseFirst(Field) + ");");
                    }

                }

                template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").remoteAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").remoteAttr(\"disabled\",\"disabled\");");

                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval) }");
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
                template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableName + "\");");
                template.AppendLine("        }");
                // update record
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
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"            {name.Replace("Id", "Key")}: {name.Replace("Id", "Key")}.val(),");
                        }
                        else
                        {
                            template.AppendLine($"            {name}: {name}.val(),");
                        }
                    }
                }
                // loop here
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                // flip the update button enabbled
                // flip the delete button enable
                // flip the create button disabled
                template.AppendLine("            $(\"#createButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").remoteAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").remoteAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success')");
                template.AppendLine("           } else if (status === false) {");
                template.AppendLine("            if (typeof(code) === 'string'){");
                template.AppendLine("            @{");
                template.AppendLine("             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("              else");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine("            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("              Swal.showLoading();");
                template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("               timerInterval = setInterval(() => {");
                template.AppendLine("               b.textContent = Swal.getTimerLeft()");
                template.AppendLine("              }, 100)");
                template.AppendLine("             },");
                template.AppendLine("             willClose: () => {");
                template.AppendLine("              clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                if(primaryKey!= null)
                template.AppendLine("             " + LowerCaseFirst(primaryKey.Replace("Id","Key"))+ ": $(\"#" + LowerCaseFirst(primaryKey.Replace("Id","Key")) + "\").val()");
                template.AppendLine("            }, statusCode: {");
                template.AppendLine("             500: function () {");
                template.AppendLine("              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("            $(\"#createButton\").remoteAttr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#updateButton\").attr(\"disabled\",\"disabled\");");
                template.AppendLine("            $(\"#deleteButton\").attr(\"disabled\",\"disabled\");");
                foreach (var fieldName in fieldNameList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;


                    if (name.Contains("Id"))
                    {
                        template.AppendLine($"\t{name.Replace("Id", "Key")}.val('');");
                    }
                    else
                    {
                        template.AppendLine("\t" + name + ".val('');");
                    }

                }
                template.AppendLine("               $(\"#" + lcTableName + "-\" + " + lcTableName + "Key).val('');");
                template.AppendLine("               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
            public string GenerateMasterDetailPages(string tableName, string tableNameDetail, string module)
            {
                var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);

                var ucTableNameDetail = GetStringNoUnderScore(tableNameDetail, (int)TextCase.UcWords);
                var lcTableNameDetail = GetStringNoUnderScore(tableNameDetail, (int)TextCase.LcWords);

                List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
                List<DescribeTableModel> describeTableDetailsModels = GetTableStructure(tableNameDetail);
                List<string?> fieldNameDetailList = describeTableDetailsModels.Select(x => x.FieldValue).ToList();

                // get PrimaryKey

                var primaryKey = GetPrimayKeyTableName(tableName);

                StringBuilder template = new();

                template.AppendLine("@inject Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor");
                template.AppendLine($"@using RebelCmsTemplate.Models.{module}");
                // this is may optional if included setting
                template.AppendLine($"@using RebelCmsTemplate.Models.Setting");
                template.AppendLine("@using RebelCmsTemplate.Models.Shared");
                template.AppendLine($"@using RebelCmsTemplate.Repository.{module}");
                template.AppendLine($"@using RebelCmsTemplate.Repository.Setting");

                template.AppendLine("@using RebelCmsTemplate.Util;");
                template.AppendLine("@using RebelCmsTemplate.Enum;");
                template.AppendLine("@{");
                template.AppendLine("    SharedUtil sharedUtils = new(_httpContextAccessor);");
                // master mostly single not list . unless it wass list search 
                template.AppendLine($"{ucTableName}Model = new();");
                // detail is many 
                template.AppendLine($"    List<{ucTableNameDetail}Model> {lcTableName}Models = new();");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        // do nothing here 
                        if (!Field.Equals("tenantId"))
                            template.AppendLine($"    List<{UpperCaseFirst(Field.Replace("Id", ""))}Model> {LowerCaseFirst(Field.Replace("Id", ""))}Models = new();");

                    }
                }
                foreach (DescribeTableModel describeTableModel in describeTableDetailsModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        // do nothing here 
                        if (!Field.Equals("tenantId"))
                            template.AppendLine($"    List<{UpperCaseFirst(Field.Replace("Id", ""))}Model> {LowerCaseFirst(Field.Replace("Id", ""))}Models = new();");

                    }
                }
                template.AppendLine("    try");
                template.AppendLine("    {");
                template.AppendLine($"       {ucTableName}Repository {lcTableName}Repository = new(_httpContextAccessor);");
                template.AppendLine($"       {lcTableName}Models = {lcTableName}Repository.Read();");

                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        if (!Field.Equals("tenantId"))
                        {
                            template.AppendLine($"       {UpperCaseFirst(Field.Replace("Id", ""))}Repository {LowerCaseFirst(Field.Replace("Id", ""))}Repository = new(_httpContextAccessor);");
                            template.AppendLine($"       {LowerCaseFirst(Field.Replace("Id", ""))}Models = {LowerCaseFirst(Field.Replace("Id", ""))}Repository.Read();");
                        }

                    }
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
                template.AppendLine("                <nav aria-label=\"breadcrumb\" class=\"breadcrumb-header float-start float-lg-end\">");
                template.AppendLine("                    <ol class=\"breadcrumb\">");
                template.AppendLine("                        <li class=\"breadcrumb-item\">");
                template.AppendLine("                            <a href=\"#\">");
                template.AppendLine("                                <i class=\"@navigationModel.FolderIcon\"></i> @navigationModel.FolderName");
                template.AppendLine("                            </a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"@navigationModel.LeafIcon\"></i> @navigationModel.LeafName");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
                template.AppendLine("                            <i class=\"fas fa-file-excel\"></i>");
                template.AppendLine("                            <a href=\"#\" onclick=\"excelRecord()\">Excel</a>");
                template.AppendLine("                        </li>");
                template.AppendLine("                        <li class=\"breadcrumb-item active\" aria-current=\"page\">");
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
                template.AppendLine("                    </div>");
                template.AppendLine("                    <div class=\"card-body\">");
                template.AppendLine("         <div class=\"row\">");
                template.AppendLine("             <div class=\"col-md-12\">");
                // start  form here
                int d = 0;
                int i = 0;
                int total = describeTableModels.Count;
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Key.Equals("PRI"))
                        {
                            // do nothing here



                        }
                        else if (Key.Equals("MUL"))
                        {
                            if (!Field.Equals("tenantId"))
                            {
                                template.AppendLine("\t<div class=\"form-group\"");
                                template.AppendLine($"\t\t<label for=\"{Field.Replace("Id", "Key")}\">{Field.Replace("Id", "")}</label>");
                                template.AppendLine($"\t\t<select id=\"{Field.Replace("Id", "Key")}\" class=\"form-control\">");
                                template.AppendLine($"\t\t @if (row.{UpperCaseFirst(Field)} == 0)");
                                template.AppendLine("\t\t {");
                                template.AppendLine("\t\t <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("\t\t }");
                                template.AppendLine("\t\t else");
                                template.AppendLine("\t\t{");
                                template.AppendLine("                       foreach (var option in from row" + UpperCaseFirst(Field.Replace("Id", "")) + " in " + LowerCaseFirst(Field.Replace("Id", "")) + "Models");
                                template.AppendLine("                        let selected = row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key ==");
                                template.AppendLine("                       row." + UpperCaseFirst(Field.Replace("Id", "")) + "Key");
                                template.AppendLine("                       select selected ? Html.Raw(\"<option value='\" +");
                                template.AppendLine("                       row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key + \"' selected>\" +");
                                template.AppendLine("                       row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Name + \"</option>\") :");
                                template.AppendLine("                       Html.Raw(\"<option value=\\\"\" + row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key +");
                                template.AppendLine("                       \"\\\">\" + row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Name + \"</option>\"))");
                                template.AppendLine("\t\t {");
                                template.AppendLine("\t\t     @option");
                                template.AppendLine("\t\t } ");
                                template.AppendLine("\t } ");

                                template.AppendLine("\t\t</select>");
                                template.AppendLine("\t</div>");

                            }

                        }
                        else if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("<div class=\"form-group\">");
                            template.AppendLine($"\t<label for=\"{Field}\"></label>");
                            template.AppendLine($"\t<input type=\"number\" id=\"{Field}\"  value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}\" class=\"form-control\" placeholder=\"\" />");
                            template.AppendLine("</div>");
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("<div class=\"form-group\">");
                            template.AppendLine($"\t<label for=\"{Field}\"></label>");
                            template.AppendLine($"\t<input type=\"number\" step=\"0.01\" id=\"{Field}\" value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("</div>");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\"></label>");
                                template.AppendLine($"\t<input type=\"datetime-local\" id=\"{Field}\" value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}.ToString(\"yyyy-MM-ddTHH:mm:ss\")\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\"></label>");
                                template.AppendLine($"\t<input type=\"date\" id=\"{Field}\" value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}.ToString(\"yyyy-MM-dd\")\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\"></label>");
                                template.AppendLine($"\t<input type=\"time\" id=\"{Field}\" value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}.ToString(\"HH:mm:ss\")\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\"></label>");
                                template.AppendLine($"\t<input type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" id=\"{Field}\" value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}.ToString(\"yyyy\")\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                            }
                            else
                            {
                                template.AppendLine("<div class=\"form-group\">");
                                template.AppendLine($"\t<label for=\"{Field}\"></label>");
                                template.AppendLine($"\t<input type=\"text\" id=\"{Field}\" value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                                template.AppendLine("</div>");
                            }
                        }
                        else
                        {
                            template.AppendLine("<div class=\"form-group\">");
                            template.AppendLine($"\t<label for=\"{Field}\"></label>");
                            template.AppendLine($"\t<input type=\"text\" id=\"{Field}\" value=\"@{ucTableName}Model.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("</div>");
                        }
                    }
                    if (d == 2)
                    {
                        template.AppendLine("             </div>");
                        template.AppendLine("        </div>");
                        // last line might not generated.generated for first loop only
                        if (i != total)
                        {
                            template.AppendLine("         <div class=\"row\">");
                            template.AppendLine("             <div class=\"col-md-12\">");
                        }
                        d = 0;
                    }
                    i++;
                }
                // loop here
                // end form here 
                template.AppendLine("                    </div>");
                template.AppendLine("                </div>");
                template.AppendLine("                <div class=\"row\">");
                template.AppendLine($"                    <div class=\"col-xs-12 col-sm-12 col-md-12\">&nbsp; <input type=\"hidden\" id=\"@{ucTableName}Model.{UpperCaseFirst(primaryKey)}\" /></div>");
                template.AppendLine("                </div>");
                template.AppendLine("            </form>");
                template.AppendLine("            <div class=\"row\">");
                template.AppendLine("                <div class=\"col-xs-12 col-sm-12 col-md-12\">");
                template.AppendLine("                    <div class=\"card\">");
                template.AppendLine("                        <table class=\"table table-bordered table-striped table-condensed table-hover\" id=\"tableData\">");
                template.AppendLine("                            <thead>");
                template.AppendLine("                                <tr>");
                // loop here
                foreach (DescribeTableModel describeTableModel in describeTableDetailsModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Key.Equals("PRI"))
                        {
                            // do nothing here 

                        }
                        else if (Key.Equals("MUL"))
                        {
                            if (!Field.Equals("tenantId"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <select name=\"{Field.Replace("Id", "Key")}\" id=\"{Field.Replace("Id", "Key")}\" class=\"form-control\">");
                                template.AppendLine($"                                              @if ({Field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("                                                {");
                                template.AppendLine("                                                  <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                                else");
                                template.AppendLine("                                                {");
                                template.AppendLine($"                                                foreach (var row" + UpperCaseFirst(Field.Replace("Id", "")) + " in " + LowerCaseFirst(Field.Replace("Id", "")) + "Models)");
                                template.AppendLine("                                                {");
                                template.AppendLine($"                                                   <option value=\"@row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key\">");
                                template.AppendLine("                                                   @row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Name</option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                               }");

                                template.AppendLine("                                             </select>");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                        }
                        else if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" step=\"0.01\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"datetime-local\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"date\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"time\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"number\" type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                        }
                        else
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }

                    }
                }
                // end loop
                template.AppendLine("                                    <td style=\"text-align: center\">");
                template.AppendLine("                                        <Button type=\"button\" class=\"btn btn-info\" onclick=\"createRecord()\">");
                template.AppendLine("                                            <i class=\"fa fa-newspaper\"></i>&nbsp;&nbsp;CREATE");
                template.AppendLine("                                        </Button>");
                template.AppendLine("                                    </td>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                                <tr>");
                foreach (DescribeTableModel describeTableModel in describeTableDetailsModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (!Key.Equals("PRI"))
                        {
                            template.AppendLine("                                    <th>" + Field.Replace(lcTableName, "").Replace("Id", "") + "</th>");
                        }
                    }

                }
                template.AppendLine("                                    <th style=\"width: 230px\">Process</th>");
                template.AppendLine("                                </tr>");
                template.AppendLine("                            </thead>");
                template.AppendLine("                            <tbody id=\"tableBody\">");
                template.AppendLine($"                                @foreach (var row in {lcTableNameDetail}Models)");
                template.AppendLine("                                {");
                template.AppendLine($"                                    <tr id='{lcTableName}-@row.{ucTableNameDetail}Key'>");
                /// loop here 
                foreach (DescribeTableModel describeTableModel in describeTableDetailsModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (Key.Equals("PRI"))
                        {
                            // do nothing here 

                        }
                        else if (Key.Equals("MUL"))
                        {
                            if (!Field.Equals("tenantId"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <select name=\"{Field.Replace("Id", "Key")}\" id=\"{Field.Replace("Id", "Key")}-@row.{ucTableNameDetail}Key\" class=\"form-control\">");
                                template.AppendLine($"                                              @if ({Field.Replace("Id", "")}Models.Count == 0)");
                                template.AppendLine("                                                {");
                                template.AppendLine("                                                  <option value=\"\">Please Create A New field </option>");
                                template.AppendLine("                                                }");
                                template.AppendLine("                                                else");
                                template.AppendLine("                                                {");
                                template.AppendLine("                       foreach (var option in from row" + UpperCaseFirst(Field.Replace("Id", "")) + " in " + LowerCaseFirst(Field.Replace("Id", "")) + "Models");
                                template.AppendLine("                        let selected = row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key ==");
                                template.AppendLine("                       row." + UpperCaseFirst(Field.Replace("Id", "")) + "Key");
                                template.AppendLine("                       select selected ? Html.Raw(\"<option value='\" +");
                                template.AppendLine("                       row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key + \"' selected>\" +");
                                template.AppendLine("                       row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Name + \"</option>\") :");
                                template.AppendLine("                       Html.Raw(\"<option value=\\\"\" + row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Key +");
                                template.AppendLine("                       \"\\\">\" + row" + UpperCaseFirst(Field.Replace("Id", "")) + "." + UpperCaseFirst(Field.Replace("Id", "")) + "Name + \"</option>\"))");
                                template.AppendLine(" {");
                                template.AppendLine("     @option");
                                template.AppendLine("                                  }");
                                template.AppendLine("                                               }");

                                template.AppendLine("                                             </select>");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }

                        }
                        else if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\"  value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"number\" step=\"0.01\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\"  value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"datetime-local\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"yyyy-MM-ddTHH:mm:ss\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"date\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"yyyy-MM-dd\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"time\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"HH:mm:ss\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"number\" min=\"1900\" max=\"2099\" step=\"1\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\" value=\"@row.{UpperCaseFirst(Field)}.ToString(\"yyyy\")\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                            else
                            {
                                template.AppendLine("                                    <td>");
                                template.AppendLine("                                        <label>");
                                template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                                template.AppendLine("                                        </label>");
                                template.AppendLine("                                    </td>");
                            }
                        }
                        else
                        {
                            template.AppendLine("                                    <td>");
                            template.AppendLine("                                        <label>");
                            template.AppendLine($"                                            <input type=\"text\" name=\"{Field}\" id=\"{Field}-@row.{ucTableNameDetail}Key\" value=\"@row.{UpperCaseFirst(Field)}\" class=\"form-control\" />");
                            template.AppendLine("                                        </label>");
                            template.AppendLine("                                    </td>");
                        }
                    }


                }
                // loop here
                template.AppendLine("                                        <td style=\"text-align: center\">");
                template.AppendLine("                                            <div class=\"btn-group\">");
                template.AppendLine($"                                                <Button type=\"button\" class=\"btn btn-warning\" onclick=\"updateRecord(@row.{ucTableNameDetail}Key)\">");
                template.AppendLine("                                                    <i class=\"fas fa-edit\"></i>&nbsp;UPDATE");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                                &nbsp;");
                template.AppendLine($"                                                <Button type=\"button\" class=\"btn btn-danger\" onclick=\"deleteRecord(@row.{ucTableNameDetail}Key)\">");
                template.AppendLine("                                                    <i class=\"fas fa-trash\"></i>&nbsp;DELETE");
                template.AppendLine("                                                </Button>");
                template.AppendLine("                                            </div>");
                template.AppendLine("                                       </td>");
                template.AppendLine("                                    </tr>");
                template.AppendLine("                                }");
                template.AppendLine($"                                @if ({lcTableNameDetail}Models.Count == 0)");
                template.AppendLine("                                {");
                template.AppendLine("                                    <tr>");
                template.AppendLine("                                        <td colspan=\"7\" class=\"noRecord\">");
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
                template.AppendLine("    <input type=\"hidden\" id=\"firstRecordCounter\" value=\"\">");
                template.AppendLine("    <input type=\"hidden\" id=\"nextRecordCounter\" value=\"\">");
                template.AppendLine("    <input type=\"hidden\" id=\"previousRecordCounter\" value=\"\">");
                template.AppendLine("    <input type=\"hidden\" id=\"lastRecordCounter\" value=\"\">");
                template.AppendLine("   <input type=\"hidden\"  id=\"endRecordCounter\" value=\"\">");

                template.AppendLine("    </section>");
                template.AppendLine("    <script>");
                foreach (DescribeTableModel describeTableModel in describeTableDetailsModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    // later custom validator 
                    if (Key.Equals("MUL"))
                    {
                        if (!Field.Equals("tenantId"))
                            template.AppendLine(" var " + Field.Replace("Id", "") + "Models = @Json.Serialize(" + Field.Replace("Id", "") + "Models);");
                    }

                }
                StringBuilder templateField = new();
                StringBuilder oneLineTemplateField = new();
                StringBuilder createTemplateField = new();
                StringBuilder updateTemplateField = new();
                foreach (var fieldName in fieldNameDetailList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            templateField.Append("row." + name.Replace("Id", "Key") + ",");
                            oneLineTemplateField.Append(name.Replace("Id", "Key") + ",");
                        }
                        else
                        {
                            templateField.Append("row." + name + ",");
                            oneLineTemplateField.Append(name + ",");
                            createTemplateField.Append(name + ".val(),");
                        }
                    }
                };
                template.AppendLine("        function resetRecord() {");
                template.AppendLine("         readRecord();");
                template.AppendLine("         $(\"#search\").val(\"\");");
                template.AppendLine("        }");
                template.AppendLine("        function emptyTemplate() {");
                template.AppendLine("         return\"<tr><td colspan='4'>It's lonely here</td></tr>\";");
                template.AppendLine("        }");
                // remember to one row template here as function name 
                template.AppendLine("        function template(" + oneLineTemplateField.ToString().TrimEnd(',') + ") {");
                foreach (DescribeTableModel describeTableModel in describeTableDetailsModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (Key.Equals("MUL"))
                    {
                        // do nothing here 
                        if (!Field.Equals("tenantId"))
                        {
                            template.AppendLine("\tlet " + Field.Replace("Id", "Key") + "Options = \"\";");
                            template.AppendLine("\tlet i = 0;");
                            template.AppendLine("\t" + Field.Replace("Id", "") + "Models.map((row) => {");
                            template.AppendLine("\t\ti++;");
                            template.AppendLine("\t\tconst selected = (parseInt(row." + Field.Replace("Id", "Key") + ") === parseInt(" + Field.Replace("Id", "Key") + ")) ? \"selected\" : \"\";");
                            template.AppendLine("\t\t" + Field.Replace("Id", "Key") + "Options += \"<option value='\" + row." + Field.Replace("Id", "Key") + " + \"' \" + selected + \">\" + row." + Field.Replace("Id", "") + "Name +\"</option>\";");
                            template.AppendLine("\t});");
                        }
                    }
                }
                template.AppendLine("            let template =  \"\" +");
                template.AppendLine($"                \"<tr id='{lcTableName}-\" + {lcTableName}Key + \"'>\" +");
                foreach (DescribeTableModel describeTableModel in describeTableDetailsModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            if (!Key.Equals("PRI"))
                            {
                                if (!Field.Equals("tenantId"))
                                {
                                    if (Key.Equals("MUL"))
                                    {
                                        template.AppendLine("\t\t\"<td class='tdNormalAlign'>\" +");
                                        template.AppendLine("\t\t\t\" <label>\" +");
                                        template.AppendLine("\t\t\t\t\"<select id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"' class='form-control'>\";");
                                        template.AppendLine("\t\ttemplate += " + Field.Replace("Id", "Key") + "Options;");
                                        template.AppendLine("\t\ttemplate += \"</select>\" +");
                                        template.AppendLine("\t\t\"</label>\" +");
                                        template.AppendLine("\t\t\"</td>\" +");
                                    }
                                    else
                                    {
                                        template.AppendLine("                                    \"<td>\" +");
                                        template.AppendLine("                                        \"<label>\" +");
                                        template.AppendLine("                                            \"<input type='number' name='" + Field.Replace("Id", "Key") + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                                        template.AppendLine("                                        \"</label>\" +");
                                        template.AppendLine("                                    \"</td>\" +");
                                    }
                                }
                            }
                        }
                        else if (Type.Contains("decimal") || Type.Equals("double") || Type.Equals("float"))
                        {
                            template.AppendLine("                                    \"<td>\" +");
                            template.AppendLine("                                        \"<label>\" +");
                            template.AppendLine("                                            \"<input type='number' step='0.01' name='" + Field + "' id='" + Field + "-\"+" + lcTableNameDetail + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                            template.AppendLine("                                        \"</label>\" +");
                            template.AppendLine("                                    \"</td>\" +");
                        }
                        else if (GetDateDataType().Any(x => Type.Contains(x)))
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='datetime-local' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='date' name='" + LowerCaseFirst(Field) + "'  id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"' value='\"+" + LowerCaseFirst(Field) + ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='time' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"' value='\"+" + LowerCaseFirst(Field) + ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else if (Type.ToString().Contains("year"))
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='number' min='1900' max='2099' step='1' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"' value='\"+" + LowerCaseFirst(Field) + ".substring(0,10)+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                    \"</td>\" +");
                            }
                            else
                            {
                                template.AppendLine("                                    \"<td>\" +");
                                template.AppendLine("                                        \"<label>\" +");
                                template.AppendLine("                                            \"<input type='text' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                                template.AppendLine("                                        \"</label>\" +");
                                template.AppendLine("                                   \"</td>\" +");
                            }
                        }
                        else
                        {
                            template.AppendLine("                                    \"<td>\" +");
                            template.AppendLine("                                        \"<label>\" +");
                            template.AppendLine($"                                            \"<input type='text' name='" + LowerCaseFirst(Field) + "' id='" + Field.Replace("Id", "Key") + "-\"+" + lcTableNameDetail + "Key+\"'' value='\"+" + LowerCaseFirst(Field) + "+\"' class='form-control' />\" +");
                            template.AppendLine("                                        \"</label>\" +");
                            template.AppendLine("                                    \"</td>\" +");
                        }
                    }

                }
                template.AppendLine("                \"<td style='text-align: center'><div class='btn-group'>\" +");
                template.AppendLine($"                \"<Button type='button' class='btn btn-warning' onclick='updateRecord(\" + {lcTableNameDetail}Key + \")'>\" +");
                template.AppendLine("                \"<i class='fas fa-edit'></i> UPDATE\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"&nbsp;\" +");
                template.AppendLine($"                \"<Button type='button' class='btn btn-danger' onclick='deleteRecord(\" + {lcTableNameDetail}Key + \")'>\" +");
                template.AppendLine("                \"<i class='fas fa-trash'></i> DELETE\" +");
                template.AppendLine("                \"</Button>\" +");
                template.AppendLine("                \"</div></td>\" +");
                template.AppendLine("                \"</tr>\";");
                template.AppendLine("               return template; ");
                template.AppendLine("        }");
                template.AppendLine("        function createRecord() {");
                // loop here 
                foreach (var fieldName in fieldNameDetailList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;
                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"         const {name.Replace("Id", "Key")} = $(\"#{name.Replace("Id", "Key")}\");");
                        }
                        else
                        {
                            template.AppendLine($"         const {name} = $(\"#{name}\");");
                        }
                    }
                }
                // loop here
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: 'POST',");
                template.AppendLine("           url: \"api/" + module.ToLower() + "/" + lcTableNameDetail + "\",");
                template.AppendLine("           async: false,");
                template.AppendLine("           data: {");
                template.AppendLine("            mode: 'createWithMaster',");
                template.AppendLine("            leafCheckKey: @navigationModel.LeafCheckKey,");
                // loop here
                foreach (var fieldName in fieldNameDetailList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"            {name.Replace("Id", "Key")}: {name.Replace("Id", "Key")}.val(),");
                        }
                        else
                        {
                            template.AppendLine($"            {name}: {name}.val(),");
                        }
                    }
                }
                // loop here
                template.AppendLine("           },statusCode: {");
                template.AppendLine("            500: function () {");
                template.AppendLine("             Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("             $(\"#tableBody\").prepend(template(lastInsertKey," + createTemplateField.ToString().TrimEnd(',') + "));");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("               title: 'Success!',");
                template.AppendLine("               text: '@SharedUtil.RecordCreated',");
                template.AppendLine("               icon: 'success',");
                template.AppendLine("               confirmButtonText: 'Cool'");
                template.AppendLine("             });");
                // loop here
                foreach (var fieldName in fieldNameDetailList)
                {
                    var name = string.Empty;
                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {
                        if (name.Contains("Id"))
                        {
                            template.AppendLine($"\t{name.Replace("Id", "Key")}.val('');");
                        }
                        else
                        {
                            template.AppendLine("\t" + name + ".val('');");
                        }
                    }
                }
                // loop here
                template.AppendLine("            } else if (status === false) {");
                template.AppendLine("             if (typeof(code) === 'string'){");
                template.AppendLine("             @{");
                template.AppendLine("              if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }else{");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine("            }else  if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval;");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("                Swal.showLoading();");
                template.AppendLine("                const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                timerInterval = setInterval(() => {");
                template.AppendLine("                b.textContent = Swal.getTimerLeft()");
                template.AppendLine("               }, 100)");
                template.AppendLine("              },");
                template.AppendLine("              willClose: () => {");
                template.AppendLine("               clearInterval(timerInterval)");
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
                template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                template.AppendLine("        function readRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableNameDetail + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"read\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("                  row = data.data[i];");
                // remember one line row 
                template.AppendLine("                  templateStringBuilder += template(" + templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                 }");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("                } else {");
                template.AppendLine("                 $(\"#tableBody\").html(\"\").html(emptyTemplate());");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");
                template.AppendLine("        function searchRecord() {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: \"post\",");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableNameDetail + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          contentType: \"application/x-www-form-urlencoded\",");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: \"search\",");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("           search: $(\"#search\").val()");
                template.AppendLine("          }, ");
                template.AppendLine("          statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("                 row = data.data[i];");
                // remember one line row 

                template.AppendLine("                 templateStringBuilder += template(" + templateField.ToString().TrimEnd(',') + ");");
                template.AppendLine("                }");
                template.AppendLine("                $(\"#tableBody\").html(\"\").html(templateStringBuilder);");
                template.AppendLine("               }");
                template.AppendLine("              }");
                template.AppendLine("             } else if (status === false) {");
                template.AppendLine("              if (typeof(code) === 'string'){");
                template.AppendLine("              @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => { clearInterval(timerInterval) }");
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
                template.AppendLine("          Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("         }).always(function (){");
                template.AppendLine("          console.log(\"always:complete\");    ");
                template.AppendLine("         });");
                template.AppendLine("        }");
                template.AppendLine("        function excelRecord() {");
                template.AppendLine("         window.open(\"api/" + module.ToLower() + "/" + lcTableNameDetail + "\");");
                template.AppendLine("        }");
                template.AppendLine("        function updateRecord(" + lcTableNameDetail + "Key) {");
                template.AppendLine("         $.ajax({");
                template.AppendLine("          type: 'POST',");
                template.AppendLine("          url: \"api/" + module.ToLower() + "/" + lcTableNameDetail + "\",");
                template.AppendLine("          async: false,");
                template.AppendLine("          data: {");
                template.AppendLine("           mode: 'update',");
                template.AppendLine("           leafCheckKey: @navigationModel.LeafCheckKey,");
                // loop here
                template.AppendLine("           " + lcTableName + "Key: " + lcTableNameDetail + "Key,");
                // loop not primary
                foreach (var fieldName in fieldNameDetailList)
                {
                    var name = string.Empty;

                    if (fieldName != null)
                        name = fieldName;

                    if (!GetHiddenField().Any(x => name.Contains(x)))
                    {

                        if (name.Contains("Id"))
                        {
                            if (name != lcTableName + "Id")
                                template.AppendLine($"           {name.Replace("Id", "Key")}: $(\"#{name.Replace("Id", "Key")}-\" + {lcTableNameDetail}Key).val(),");
                        }
                        else
                        {
                            template.AppendLine($"           {name}: $(\"#{name}-\" + {lcTableNameDetail}Key).val(),");
                        }
                    }
                }
                // loop here
                template.AppendLine("          }, statusCode: {");
                template.AppendLine("           500: function () {");
                template.AppendLine("            Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
                template.AppendLine("            Swal.fire(\"System\", \"@SharedUtil.RecordUpdated\", 'success')");
                template.AppendLine("           } else if (status === false) {");
                template.AppendLine("            if (typeof(code) === 'string'){");
                template.AppendLine("            @{");
                template.AppendLine("             if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("              else");
                template.AppendLine("              {");
                template.AppendLine("               <text>");
                template.AppendLine("                Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("               </text>");
                template.AppendLine("              }");
                template.AppendLine("             }");
                template.AppendLine("            }else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("             let timerInterval");
                template.AppendLine("             Swal.fire({");
                template.AppendLine("              title: 'Auto close alert!',");
                template.AppendLine("              html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("              timer: 2000,");
                template.AppendLine("              timerProgressBar: true,");
                template.AppendLine("              didOpen: () => {");
                template.AppendLine("              Swal.showLoading();");
                template.AppendLine("               const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("               timerInterval = setInterval(() => {");
                template.AppendLine("               b.textContent = Swal.getTimerLeft()");
                template.AppendLine("              }, 100)");
                template.AppendLine("             },");
                template.AppendLine("             willClose: () => {");
                template.AppendLine("              clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("          }).always(function (){");
                template.AppendLine("           console.log(\"always:complete\");    ");
                template.AppendLine("          });");
                template.AppendLine("        }");
                template.AppendLine("        function deleteRecord(" + lcTableNameDetail + "Key) { ");
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
                template.AppendLine("            url: \"api/" + module.ToLower() + "/" + lcTableNameDetail + "\",");
                template.AppendLine("            async: false,");
                template.AppendLine("            data: {");
                template.AppendLine("             mode: 'delete',");
                template.AppendLine("             leafCheckKey: @navigationModel.LeafCheckKey,");
                template.AppendLine("             " + lcTableNameDetail + "Key: " + lcTableNameDetail + "Key");
                template.AppendLine("            }, statusCode: {");
                template.AppendLine("             500: function () {");
                template.AppendLine("              Swal.fire(\"System Error\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("             }");
                template.AppendLine("            },");
                template.AppendLine("            beforeSend: function () {");
                template.AppendLine("             console.log(\"loading..\");");
                template.AppendLine("           }}).done(function(data)  {");
                template.AppendLine("              if (data === void 0) { location.href = \"/\"; }");
                template.AppendLine("              let status = data.status;");
                template.AppendLine("              let code = data.code;");
                template.AppendLine("              if (status) {");
                template.AppendLine("               $(\"#" + lcTableName + "-\" + " + lcTableName + "Key).remove();");
                template.AppendLine("               Swal.fire(\"System\", \"@SharedUtil.RecordDeleted\", \"success\");");
                template.AppendLine("              } else if (status === false) {");
                template.AppendLine("               if (typeof(code) === 'string'){");
                template.AppendLine("               @{");
                template.AppendLine("                if (sharedUtils.GetRoleId().Equals( (int)AccessEnum.ADMINISTRATOR_ACCESS ))");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"Debugging Admin\", code, \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("                else");
                template.AppendLine("                {");
                template.AppendLine("                 <text>");
                template.AppendLine("                  Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
                template.AppendLine("                 </text>");
                template.AppendLine("                }");
                template.AppendLine("               }");
                template.AppendLine("              } else if (parseInt(code) === parseInt(@((int)ReturnCodeEnum.ACCESS_DENIED) )) {");
                template.AppendLine("               let timerInterval;");
                template.AppendLine("               Swal.fire({");
                template.AppendLine("                title: 'Auto close alert!',");
                template.AppendLine("                html: 'Session Out .Pease Re-login.I will close in <b></b> milliseconds.',");
                template.AppendLine("                timer: 2000,");
                template.AppendLine("                timerProgressBar: true,");
                template.AppendLine("                didOpen: () => {");
                template.AppendLine("                 Swal.showLoading();");
                template.AppendLine("                 const b = Swal.getHtmlContainer().querySelector('b');");
                template.AppendLine("                 timerInterval = setInterval(() => {");
                template.AppendLine("                 b.textContent = Swal.getTimerLeft()");
                template.AppendLine("                }, 100)");
                template.AppendLine("               },");
                template.AppendLine("               willClose: () => {");
                template.AppendLine("                clearInterval(timerInterval)");
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
                template.AppendLine("           Swal.fire(\"System\", \"@SharedUtil.UserErrorNotification\", \"error\");");
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
            public string GenerateRepository(string tableName, string module, string tableNameMaster = "")
            {

                var primaryKey = GetPrimayKeyTableName(tableName);

                var ucTableName = GetStringNoUnderScore(tableName, (int)TextCase.UcWords);
                var lcTableName = GetStringNoUnderScore(tableName, (int)TextCase.LcWords);

                List<DescribeTableModel> describeTableModels = GetTableStructure(tableName);
                List<string?> fieldNameList = describeTableModels.Select(x => x.FieldValue).ToList();
                var sqlFieldName = String.Join(',', fieldNameList);
                List<string?> fieldNameParameter = new();
                foreach (var fieldName in fieldNameList)
                {
                    if (fieldName != null)
                    {
                        if (fieldName.Equals(lcTableName + "Id"))
                        {
                            fieldNameParameter.Add("null");
                        }
                        else
                        {
                            fieldNameParameter.Add("@" + fieldName);
                        }
                    }
                };
                var sqlBindParamFieldName = String.Join(',', fieldNameParameter);

                StringBuilder loopColumn = new();
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;

                    List<string> keyValue = new() { "PRI", "MUL" };
                    if (keyValue.Contains(Key))
                    {
                        if (Key != "PRI")
                        {
                            if (Field.Equals("tenantId"))
                            {
                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumn.AppendLine("                        Value = _sharedUtil.GetTenantId()");
                                loopColumn.AppendLine("                    },");
                            }
                            else
                            {

                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumn.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field.Replace("Id", "Key")));
                                loopColumn.AppendLine("                    },");
                            }
                        }
                    }
                    else
                    {
                        if (Field.Equals("isDelete"))
                        {
                            loopColumn.AppendLine("                    new ()");
                            loopColumn.AppendLine("                    {");
                            loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                            loopColumn.AppendLine("                        Value = 0");
                            loopColumn.AppendLine("                    },");
                        }
                        else if (Field.Equals("executeBy"))
                        {
                            loopColumn.AppendLine("                    new ()");
                            loopColumn.AppendLine("                    {");
                            loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                            loopColumn.AppendLine("                        Value = _sharedUtil.GetUserName()");
                            loopColumn.AppendLine("                    },");
                        }
                        else
                        {
                            if (Type.ToString().Contains("datetime"))
                            {
                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumn.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"yyyy-MM-dd HH:mm\")");
                                loopColumn.AppendLine("                    },");
                            }
                            else if (Type.ToString().Contains("date"))
                            {
                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumn.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"yyyy-MM-dd\")");
                                loopColumn.AppendLine("                    },");
                            }
                            else if (Type.ToString().Contains("time"))
                            {
                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumn.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"HH:mm\")");
                                loopColumn.AppendLine("                    },");
                            }
                            else
                            {
                                loopColumn.AppendLine("                    new ()");
                                loopColumn.AppendLine("                    {");
                                loopColumn.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumn.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field));
                                loopColumn.AppendLine("                    },");
                            }
                        }
                    }
                }



                StringBuilder template = new();

                template.AppendLine("using System;");
                template.AppendLine("using System.Collections.Generic;");
                template.AppendLine("using System.IO;");
                template.AppendLine("using ClosedXML.Excel;");
                template.AppendLine("using Microsoft.AspNetCore.Http;");
                template.AppendLine("using MySql.Data.MySqlClient;");
                template.AppendLine("using RebelCmsTemplate.Models." + module + ";");
                template.AppendLine("using RebelCmsTemplate.Models.Shared;");
                template.AppendLine("using RebelCmsTemplate.Util;");
                template.AppendLine("namespace RebelCmsTemplate.Repository." + module + ";");
                template.AppendLine("    public class " + ucTableName + "Repository");
                template.AppendLine("    {");
                template.AppendLine("        private readonly SharedUtil _sharedUtil;");
                template.AppendLine("        public " + ucTableName + "Repository(IHttpContextAccessor httpContextAccessor)");
                template.AppendLine("        {");
                template.AppendLine("            _sharedUtil = new SharedUtil(httpContextAccessor);");
                template.AppendLine("        }");
                template.AppendLine("        public int Create(" + ucTableName + "Model " + lcTableName + "Model)");
                template.AppendLine("        {");
                template.AppendLine("            var lastInsertKey = 0;");
                template.AppendLine("            string sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                template.AppendLine("                sql += @\"INSERT INTO " + tableName + " (" + sqlFieldName + ") VALUES (" + sqlBindParamFieldName + ");\";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                parameterModels = new List<ParameterModel>");

                template.AppendLine("                {");
                // loop start
                template.AppendLine(loopColumn.ToString().TrimEnd(','));
                // loop end
                template.AppendLine("                };");
                template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine("                   mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                template.AppendLine("                mySqlTransaction.Commit();");
                template.AppendLine("                lastInsertKey = (int)mySqlCommand.LastInsertedId;");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");
                template.AppendLine("            return lastInsertKey;");

                template.AppendLine("        }");

                if (!string.IsNullOrEmpty(tableNameMaster))
                {
                    var ucTableNameMaster = GetStringNoUnderScore(tableNameMaster, (int)TextCase.UcWords);
                    var lcTableNameMaster = GetStringNoUnderScore(tableNameMaster, (int)TextCase.LcWords);

                    List<DescribeTableModel> describeTableMasterModels = GetTableStructure(tableNameMaster);
                    List<string?> fieldNameMasterList = describeTableMasterModels.Select(x => x.FieldValue).ToList();
                    var sqlFieldNameMaster = String.Join(',', fieldNameMasterList);
                    List<string?> fieldNameMasterParameter = new();
                    foreach (var fieldName in fieldNameMasterList)
                    {
                        if (fieldName != null)
                        {
                            if (fieldName.Equals(lcTableName + "Id"))
                            {
                                fieldNameMasterParameter.Add("null");
                            }
                            else
                            {
                                fieldNameMasterParameter.Add("@" + fieldName);
                            }
                        }
                    };
                    var sqlBindParamFieldNameMaster = String.Join(',', fieldNameMasterParameter);
                    StringBuilder loopColumnDetail = new();
                    foreach (DescribeTableModel describeTableModel in describeTableModels)
                    {
                        string Key = string.Empty;
                        string Field = string.Empty;
                        string Type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                            Key = describeTableModel.KeyValue;
                        if (describeTableModel.FieldValue != null)
                            Field = describeTableModel.FieldValue;
                        if (describeTableModel.TypeValue != null)
                            Type = describeTableModel.TypeValue;

                        List<string> keyValue = new() { "PRI", "MUL" };
                        if (keyValue.Contains(Key))
                        {
                            if (Key != "PRI")
                            {
                                if (Field.Equals("tenantId"))
                                {
                                    loopColumnDetail.AppendLine("                    new ()");
                                    loopColumnDetail.AppendLine("                    {");
                                    loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnDetail.AppendLine("                        Value = _sharedUtil.GetTenantId()");
                                    loopColumnDetail.AppendLine("                    },");
                                }
                                else
                                {
                                    // so we have foreign key here so .. master need to link with the detail
                                    // what if we have mutiple master / detail  / detail / detail ? Cleanup your design layout
                                    var masterPrimaryKey = GetPrimayKeyTableName(tableNameMaster);
                                    if (masterPrimaryKey != null)
                                    {
                                        if (masterPrimaryKey.Equals(Field, StringComparison.Ordinal))
                                        {
                                            loopColumnDetail.AppendLine("                    new ()");
                                            loopColumnDetail.AppendLine("                    {");
                                            loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                            loopColumnDetail.AppendLine("                        Value = lastInsertKey");
                                            loopColumnDetail.AppendLine("                    },");
                                        }
                                    }
                                    else
                                    {
                                        loopColumnDetail.AppendLine("                    new ()");
                                        loopColumnDetail.AppendLine("                    {");
                                        loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                        loopColumnDetail.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field.Replace("Id", "Key")));
                                        loopColumnDetail.AppendLine("                    },");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Field.Equals("isDelete"))
                            {
                                loopColumnDetail.AppendLine("                    new ()");
                                loopColumnDetail.AppendLine("                    {");
                                loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumnDetail.AppendLine("                        Value = 0");
                                loopColumnDetail.AppendLine("                    },");
                            }
                            else if (Field.Equals("executeBy"))
                            {
                                loopColumnDetail.AppendLine("                    new ()");
                                loopColumnDetail.AppendLine("                    {");
                                loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumnDetail.AppendLine("                        Value = _sharedUtil.GetUserName()");
                                loopColumnDetail.AppendLine("                    },");
                            }
                            else
                            {
                                if (Type.ToString().Contains("datetime"))
                                {
                                    loopColumnDetail.AppendLine("                    new ()");
                                    loopColumnDetail.AppendLine("                    {");
                                    loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnDetail.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"yyyy-MM-dd HH:mm\")");
                                    loopColumnDetail.AppendLine("                    },");
                                }
                                else if (Type.ToString().Contains("date"))
                                {
                                    loopColumnDetail.AppendLine("                    new ()");
                                    loopColumnDetail.AppendLine("                    {");
                                    loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnDetail.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"yyyy-MM-dd\")");
                                    loopColumnDetail.AppendLine("                    },");
                                }
                                else if (Type.ToString().Contains("time"))
                                {
                                    loopColumnDetail.AppendLine("                    new ()");
                                    loopColumnDetail.AppendLine("                    {");
                                    loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumn.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"HH:mm\")");
                                    loopColumn.AppendLine("                    },");
                                }
                                else
                                {
                                    loopColumnDetail.AppendLine("                    new ()");
                                    loopColumnDetail.AppendLine("                    {");
                                    loopColumnDetail.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnDetail.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field));
                                    loopColumnDetail.AppendLine("                    },");
                                }
                            }
                        }
                    }

                    StringBuilder loopColumnMaster = new();
                    foreach (DescribeTableModel describeTableModel in describeTableMasterModels)
                    {
                        string Key = string.Empty;
                        string Field = string.Empty;
                        string Type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                            Key = describeTableModel.KeyValue;
                        if (describeTableModel.FieldValue != null)
                            Field = describeTableModel.FieldValue;
                        if (describeTableModel.TypeValue != null)
                            Type = describeTableModel.TypeValue;

                        List<string> keyValue = new() { "PRI", "MUL" };
                        if (keyValue.Contains(Key))
                        {
                            if (Key != "PRI")
                            {
                                if (Field.Equals("tenantId"))
                                {
                                    loopColumnMaster.AppendLine("                    new ()");
                                    loopColumnMaster.AppendLine("                    {");
                                    loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnMaster.AppendLine("                        Value = _sharedUtil.GetTenantId()");
                                    loopColumnMaster.AppendLine("                    },");
                                }
                                else
                                {

                                    loopColumnMaster.AppendLine("                    new ()");
                                    loopColumnMaster.AppendLine("                    {");
                                    loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnMaster.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field.Replace("Id", "Key")));
                                    loopColumnMaster.AppendLine("                    },");
                                }
                            }
                        }
                        else
                        {
                            if (Field.Equals("isDelete"))
                            {
                                loopColumnMaster.AppendLine("                    new ()");
                                loopColumnMaster.AppendLine("                    {");
                                loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumnMaster.AppendLine("                        Value = 0");
                                loopColumnMaster.AppendLine("                    },");
                            }
                            else if (Field.Equals("executeBy"))
                            {
                                loopColumnMaster.AppendLine("                    new ()");
                                loopColumnMaster.AppendLine("                    {");
                                loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                loopColumnMaster.AppendLine("                        Value = _sharedUtil.GetUserName()");
                                loopColumnMaster.AppendLine("                    },");
                            }
                            else
                            {
                                if (Type.ToString().Contains("datetime"))
                                {
                                    loopColumnMaster.AppendLine("                    new ()");
                                    loopColumnMaster.AppendLine("                    {");
                                    loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnMaster.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"yyyy-MM-dd HH:mm\")");
                                    loopColumnMaster.AppendLine("                    },");
                                }
                                else if (Type.ToString().Contains("date"))
                                {
                                    loopColumn.AppendLine("                    new ()");
                                    loopColumnMaster.AppendLine("                    {");
                                    loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnMaster.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"yyyy-MM-dd\")");
                                    loopColumnMaster.AppendLine("                    },");
                                }
                                else if (Type.ToString().Contains("time"))
                                {
                                    loopColumnMaster.AppendLine("                    new ()");
                                    loopColumnMaster.AppendLine("                    {");
                                    loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnMaster.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field) + ".ToString(\"HH:mm\")");
                                    loopColumnMaster.AppendLine("                    },");
                                }
                                else
                                {
                                    loopColumnMaster.AppendLine("                    new ()");
                                    loopColumnMaster.AppendLine("                    {");
                                    loopColumnMaster.AppendLine("                        Key = \"@" + Field + "\",");
                                    loopColumnMaster.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(Field));
                                    loopColumnMaster.AppendLine("                    },");
                                }
                            }
                        }
                    }

                    template.AppendLine("        public int CreateWithMaster(" + ucTableName + "Model " + lcTableName + "Model," + ucTableNameMaster + "Model " + lcTableNameMaster + "Model)");
                    template.AppendLine("        {");
                    // insert the master table first 
                    template.AppendLine("            var lastInsertKey = 0;");
                    template.AppendLine("            string sql = string.Empty;");
                    template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                    template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                    template.AppendLine("            try");
                    template.AppendLine("            {");
                    template.AppendLine("                connection.Open();");
                    template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                    template.AppendLine("                sql += @\"INSERT INTO " + tableNameMaster + " (" + sqlFieldNameMaster + ") VALUES (" + sqlBindParamFieldNameMaster + ");\";");
                    template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                    template.AppendLine("                parameterModels = new List<ParameterModel>");

                    template.AppendLine("                {");
                    // loop start
                    template.AppendLine(loopColumnMaster.ToString().TrimEnd(','));
                    // loop end
                    template.AppendLine("                };");
                    template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
                    template.AppendLine("                {");
                    template.AppendLine("                   mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                    template.AppendLine("                }");
                    template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                    template.AppendLine("                mySqlTransaction.Commit();");
                    template.AppendLine("                lastInsertKey = (int)mySqlCommand.LastInsertedId;");
                    template.AppendLine("                mySqlCommand.Dispose();");
                    template.AppendLine("            }");
                    template.AppendLine("            catch (MySqlException ex)");
                    template.AppendLine("            {");
                    template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                    template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                    template.AppendLine("                throw new Exception(ex.Message);");
                    template.AppendLine("            }");


                    // insert the detail table 
                    template.AppendLine("            string sql = string.Empty;");
                    template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                    template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                    template.AppendLine("            try");
                    template.AppendLine("            {");
                    template.AppendLine("                connection.Open();");
                    template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                    template.AppendLine("                sql += @\"INSERT INTO " + tableName + " (" + sqlFieldName + ") VALUES (" + sqlBindParamFieldName + ");\";");
                    template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                    template.AppendLine("                parameterModels = new List<ParameterModel>");

                    template.AppendLine("                {");
                    // loop start
                    template.AppendLine(loopColumnDetail.ToString().TrimEnd(','));
                    // loop end
                    template.AppendLine("                };");
                    template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
                    template.AppendLine("                {");
                    template.AppendLine("                   mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                    template.AppendLine("                }");
                    template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                    template.AppendLine("                mySqlTransaction.Commit();");
                    template.AppendLine("                lastInsertKey = (int)mySqlCommand.LastInsertedId;");
                    template.AppendLine("                mySqlCommand.Dispose();");
                    template.AppendLine("            }");
                    template.AppendLine("            catch (MySqlException ex)");
                    template.AppendLine("            {");
                    template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                    template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                    template.AppendLine("                throw new Exception(ex.Message);");
                    template.AppendLine("            }");

                    template.AppendLine("            return lastInsertKey;");
                    template.AppendLine("}");

                }
                template.AppendLine("        public List<" + ucTableName + "Model> Read()");
                template.AppendLine("        {");
                template.AppendLine("            List<" + ucTableName + "Model> " + lcTableName + "Models = new();");
                template.AppendLine("            string sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine("                sql = @\"");
                template.AppendLine("                SELECT      *");
                template.AppendLine("                FROM        " + tableName + " ");
                template.AppendLine("                WHERE       isDelete !=1");
                template.AppendLine("                ORDER BY    " + primaryKey + " DESC \";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    while (reader.Read())");
                template.AppendLine("                    {");

                template.AppendLine("                        " + lcTableName + "Models.Add(new " + ucTableName + "Model");
                template.AppendLine("                       {");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (GetNumberDataType().Any(x => Type.Contains(x)))
                    {
                        if (Key.Equals("PRI") || Key.Equals("MUL"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field.Replace("Id", "Key") + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field)) + "\"]),");
                        }
                        else
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                    }
                    else if (Type.Contains("double") || Type.Contains("float"))
                    {
                        template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToDouble(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                    }
                    else if (Type.Contains("decimal"))
                    {
                        template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToDecimal(reader[\"" + LowerCaseFirst(Field) + "\"]),");

                    }
                    else if (GetDateDataType().Any(x => Type.Contains(x)))
                    {

                        if (Type.ToString().Contains("datetime"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToDateTime(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("year"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("time"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = CustomDateTimeConvert.ConvertToTime((TimeSpan)reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("date"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                      

                    }  else if (GetBlobType().Any(x => Type.Contains(x)))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = (byte[])reader[\"" + LowerCaseFirst(Field) + "\"],");
                        }
                        else
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = reader[\"" + LowerCaseFirst(Field) + "\"].ToString(),");
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
                template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("               throw new Exception(ex.Message);");
                template.AppendLine("            }");

                template.AppendLine("            return " + lcTableName + "Models;");
                template.AppendLine("        }");
                // search method/function
                template.AppendLine("        public List<" + ucTableName + "Model> Search(string search)");
                template.AppendLine("       {");
                template.AppendLine("            List<" + ucTableName + "Model> " + lcTableName + "Models = new();");
                template.AppendLine("            string sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                // the main problem is are we should filter em all or else ? 
                template.AppendLine("                connection.Open();");
                template.AppendLine("                sql += @\"");
                template.AppendLine("                SELECT  *");
                template.AppendLine("                FROM    " + tableName + " ");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            if (Key.Equals("MUL"))
                            {
                                // seem replacing id not suitable because we forgeting the camel case . so grab the table name from the system it self 
                                template.AppendLine("\t JOIN " + GetForeignKeyTableName(tableName, Field) + " ");
                                template.AppendLine("\t USING(" + Field + ")");
                            }
                        }
                    }
                }
                // how many table join is related ? 
                template.AppendLine($"\t WHERE   {tableName}.isDelete != 1");
                // we create a list which field  manually so end user can choose we give em all filter 
                StringBuilder templateSearch = new();
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {

                        if (Key.Equals("MUL"))
                        {

                            var foreignTableName = GetForeignKeyTableName(tableName, Field);
                            if (foreignTableName != null)
                            {
                                List<DescribeTableModel> describeTableForeignModels = GetTableStructure(foreignTableName);
                                foreach (DescribeTableModel describeTableForeignModel in describeTableForeignModels)
                                {
                                    string ForeignModelKey = string.Empty;
                                    string ForeignModelField = string.Empty;
                                    string ForeignModelType = string.Empty;
                                    if (describeTableModel.KeyValue != null)
                                        ForeignModelKey = describeTableModel.KeyValue;
                                    if (describeTableModel.FieldValue != null)
                                        ForeignModelField = describeTableModel.FieldValue;
                                    if (describeTableModel.TypeValue != null)
                                        ForeignModelType = describeTableModel.TypeValue;
                                    if (!(ForeignModelKey.Equals("PRI") && ForeignModelKey.Equals("MULL")))
                                        templateSearch.Append("\t " + foreignTableName + "." + ForeignModelField + " like concat('%',@search,'%') OR");

                                }
                            }
                        }
                        else
                        {
                            if (!Key.Equals("PRI"))
                                templateSearch.Append("\n\t " + tableName + "." + Field + " like concat('%',@search,'%') OR");
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
                template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    while (reader.Read())");
                template.AppendLine("                   {");
                template.AppendLine("                        " + lcTableName + "Models.Add(new " + ucTableName + "Model");
                template.AppendLine("                       {");


                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (GetNumberDataType().Any(x => Type.Contains(x)))
                    {
                        if (Key.Equals("PRI") || Key.Equals("MUL"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field.Replace("Id", "Key") + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field)) + "\"]),");
                        }
                        else
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                    }
                    else if (Type.Contains("double") || Type.Contains("float"))
                    {
                        template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToDouble(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                    }
                    else if (Type.Contains("decimal"))
                    {
                        template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToDecimal(reader[\"" + LowerCaseFirst(Field) + "\"]),");

                    }
                    else if (GetDateDataType().Any(x => Type.Contains(x)))
                    {
                        // year allready as int 
                        if (Type.ToString().Contains("datetime"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToDateTime(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("year"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("time"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = CustomDateTimeConvert.ConvertToTime((TimeSpan)reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("date"))
                        {
                            template.AppendLine("                            " + UpperCaseFirst(Field) + " = CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }

                    }
                    else if (GetBlobType().Any(x => Type.Contains(x)))
                    {
                        template.AppendLine("                            " + UpperCaseFirst(Field) + " = (byte[])reader[\"" + LowerCaseFirst(Field) + "\"],");
                    }
                    else
                    {
                        template.AppendLine("                            " + UpperCaseFirst(Field) + " = reader[\"" + LowerCaseFirst(Field) + "\"].ToString(),");
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
                template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");

                template.AppendLine("            return " + lcTableName + "Models;");
                template.AppendLine("        }");
                // View One Recond aka findViewById 

                template.AppendLine("        public " + ucTableName + "Model  GetSingle(" + ucTableName + "Model " + lcTableName + "Model)");
                template.AppendLine("        {");
                // we can used single  but the above parameter it is the same value . Yeah oop can be a double sword edges . 
                // We can remove the line also 
                template.AppendLine("            string sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                // the main problem is are we should filter em all or else ? 
                template.AppendLine("                connection.Open();");
                template.AppendLine("                sql += @\"");
                template.AppendLine("                SELECT  *");
                template.AppendLine("                FROM    " + tableName + " ");

                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (!GetHiddenField().Any(x => Field.Contains(x)))
                    {
                        if (GetNumberDataType().Any(x => Type.Contains(x)))
                        {
                            if (Key.Equals("MUL"))
                            {
                                template.AppendLine("\t JOIN " + GetForeignKeyTableName(tableName, Field) + " ");
                                template.AppendLine("\t USING(" + Field + ")");
                            }
                        }
                    }
                }
                // how many table join is related ? 
                template.AppendLine($"\t WHERE   {tableName}.isDelete != 1");
                template.AppendLine("                WHERE   " + tableName + "." + primaryKey + "    =   @" + primaryKey + " LIMIT 1\";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                parameterModels = new List<ParameterModel>");
                template.AppendLine("                {");
                template.AppendLine("                    new ()");
                template.AppendLine("                    {");
                template.AppendLine("                        Key = \"@" + primaryKey + "\",");
                if (primaryKey != null)
                    template.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(primaryKey.Replace("Id", "Key")));
                template.AppendLine("                   }");
                template.AppendLine("                };");

                template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                _sharedUtil.SetSqlSession(sql, parameterModels); ");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    while (reader.Read())");
                template.AppendLine("                   {");

                // we cannot using  Model.field = "value" as using init in the model 

                template.AppendLine(lcTableName+"Model = new "+ucTableName+"Model() { ");
                foreach (DescribeTableModel describeTableModel in describeTableModels)
                {
                    string Key = string.Empty;
                    string Field = string.Empty;
                    string Type = string.Empty;
                    if (describeTableModel.KeyValue != null)
                        Key = describeTableModel.KeyValue;
                    if (describeTableModel.FieldValue != null)
                        Field = describeTableModel.FieldValue;
                    if (describeTableModel.TypeValue != null)
                        Type = describeTableModel.TypeValue;


                    if (GetNumberDataType().Any(x => Type.Contains(x)))
                    {
                        if (Key.Equals("PRI") || Key.Equals("MUL"))
                        {
                            template.AppendLine(UpperCaseFirst(Field.Replace("Id", "Key") + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field)) + "\"]),");
                        }
                        else
                        {
                            template.AppendLine(UpperCaseFirst(Field) + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                    }
                    else if (Type.Contains("double") || Type.Contains("float"))
                    {
                        template.AppendLine(UpperCaseFirst(Field) + " = Convert.ToDouble(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                    }
                    else if (Type.Contains("decimal"))
                    {
                        template.AppendLine(UpperCaseFirst(Field) + " = Convert.ToDecimal(reader[\"" + LowerCaseFirst(Field) + "\"]),");

                    }
                    else if (GetDateDataType().Any(x => Type.Contains(x)))
                    {
                        // year allready as int 
                        if (Type.ToString().Contains("datetime"))
                        {
                            template.AppendLine(UpperCaseFirst(Field) + " = Convert.ToDateTime(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("year"))
                        {
                            template.AppendLine(UpperCaseFirst(Field) + " = Convert.ToInt32(reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("time"))
                        {
                            template.AppendLine(UpperCaseFirst(Field) + " = CustomDateTimeConvert.ConvertToTime((TimeSpan)reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }
                        else if (Type.ToString().Contains("date"))
                        {
                            template.AppendLine(UpperCaseFirst(Field) + " = CustomDateTimeConvert.ConvertToDate((DateTime)reader[\"" + LowerCaseFirst(Field) + "\"]),");
                        }

                    }
                    else if (GetBlobType().Any(x => Type.Contains(x)))
                    {
                        template.AppendLine(UpperCaseFirst(Field) + " = (byte[])reader[\"" + LowerCaseFirst(Field) + "\"],");
                    }
                    else
                    {
                        template.AppendLine(UpperCaseFirst(Field) + " = reader[\"" + LowerCaseFirst(Field) + "\"].ToString(),");
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
                template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");

                template.AppendLine("            return " + lcTableName + "Model;");
                template.AppendLine("        }");
                // excel method/function
                template.AppendLine("        public byte[] GetExcel()");
                template.AppendLine("        {");
                template.AppendLine("            using var workbook = new XLWorkbook();");
                template.AppendLine("            var worksheet = workbook.Worksheets.Add(\"Administrator > " + ucTableName + " \");");
                // loop here
                for (int i = 0; i < fieldNameList.Count; i++)
                {
                    template.AppendLine("            worksheet.Cell(1, " + (i + 1) + ").Value = \"" + fieldNameList[i] + "\";");
                }
                // loop end

                template.AppendLine("            var sql = _sharedUtil.GetSqlSession();");
                template.AppendLine("           var parameterModels = _sharedUtil.GetListSqlParameter();");
                template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("               connection.Open();");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                if (parameterModels != null)");
                template.AppendLine("                {");
                template.AppendLine("                    foreach (ParameterModel parameter in parameterModels)");
                template.AppendLine("                    {");
                template.AppendLine("                        mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                    }");
                template.AppendLine("                }");
                template.AppendLine("                using (var reader = mySqlCommand.ExecuteReader())");
                template.AppendLine("                {");
                template.AppendLine("                    var counter = 1;");
                template.AppendLine("                   while (reader.Read())");
                template.AppendLine("                    {");
                template.AppendLine("                        var currentRow = counter++;");
                // loop here
                for (int i = 0; i < fieldNameList.Count; i++)
                {
                    template.AppendLine("                        worksheet.Cell(currentRow, 2).Value = reader[\"" + fieldNameList[i] + "\"].ToString();");
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

                template.AppendLine("            string sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                template.AppendLine("                sql = @\"");
                template.AppendLine("                UPDATE  " + tableName + " ");
                // start loop
                template.AppendLine("                SET     ");
                StringBuilder updateString = new();
                for (int i = 0; i < fieldNameList.Count; i++)
                {
                    if (fieldNameList[i] != lcTableName + "Id")
                    {
                        if (i + 1 == fieldNameList.Count)
                        {
                            updateString.AppendLine(fieldNameList[i] + "=@" + fieldNameList[i]);

                        }
                        else
                        {
                            updateString.AppendLine(fieldNameList[i] + "=@" + fieldNameList[i] + ",");
                        }
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
                    template.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(primaryKey.Replace("Id", "Key")));
                template.AppendLine("                   },");
                template.AppendLine(loopColumn.ToString().TrimEnd(','));
                // loop end
                template.AppendLine("                };");
                template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                template.AppendLine("                mySqlTransaction.Commit();");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");
                template.AppendLine("        }");
                template.AppendLine("        public void Delete(" + ucTableName + "Model " + lcTableName + "Model)");
                template.AppendLine("        {");
                template.AppendLine("            string sql = string.Empty;");
                template.AppendLine("            List<ParameterModel> parameterModels = new ();");
                template.AppendLine("            using MySqlConnection connection = SharedUtil.GetConnection();");
                template.AppendLine("            try");
                template.AppendLine("            {");
                template.AppendLine("                connection.Open();");
                template.AppendLine("                MySqlTransaction mySqlTransaction = connection.BeginTransaction();");
                // we do soft delete for easy long term audit
                template.AppendLine("                sql = @\"");
                template.AppendLine("                UPDATE  " + tableName + " ");
                template.AppendLine("                SET     isDelete    =   1");
                template.AppendLine("                WHERE   " + primaryKey + "    =   @" + primaryKey + "\";");
                template.AppendLine("                MySqlCommand mySqlCommand = new(sql, connection);");
                template.AppendLine("                parameterModels = new List<ParameterModel>");

                template.AppendLine("                {");
                template.AppendLine("                    new ()");
                template.AppendLine("                    {");
                template.AppendLine("                        Key = \"@" + lcTableName + "Id\",");
                if (primaryKey != null)
                    template.AppendLine("                        Value = " + lcTableName + "Model." + UpperCaseFirst(primaryKey.Replace("Id", "Key")));
                template.AppendLine("                   }");
                template.AppendLine("                };");
                template.AppendLine("                foreach (ParameterModel parameter in parameterModels)");
                template.AppendLine("                {");
                template.AppendLine("                    mySqlCommand.Parameters.AddWithValue(parameter.Key, parameter.Value);");
                template.AppendLine("                }");
                template.AppendLine("                mySqlCommand.ExecuteNonQuery();");
                template.AppendLine("                mySqlTransaction.Commit();");
                template.AppendLine("                mySqlCommand.Dispose();");
                template.AppendLine("            }");
                template.AppendLine("            catch (MySqlException ex)");
                template.AppendLine("            {");
                template.AppendLine("                System.Diagnostics.Debug.WriteLine(ex.Message);");
                template.AppendLine("                _sharedUtil.SetQueryException(SharedUtil.GetSqlSessionValue(sql, parameterModels), ex);");
                template.AppendLine("                throw new Exception(ex.Message);");
                template.AppendLine("            }");
                template.AppendLine("        }");
                template.AppendLine("}");

                return template.ToString(); ;
            }
            /// <summary>
            /// Before we using  {Field}Name or {Field}Description.So upon automation , we refer to latest string data type as reference much easier for long term purpose because somebody
            /// will freakin design not following order
            /// This is more on name and placeholder
            /// </summary>
            /// <param name="tableName"></param>
            /// <param name="field"></param>
            /// <returns></returns>
            public string? GetLabelOrPlaceHolderForComboBox(string tableName, string field)
            {
                string name = string.Empty;
                var foreignTableName = GetForeignKeyTableName(tableName, field);
                // we describe the table name to get the any varchar /text which found first . If yes desclare as the name for combo box /select box
                if (foreignTableName != null)
                {
                    var structureModel = GetTableStructure(foreignTableName);
                    foreach (DescribeTableModel describeTableModel in structureModel)
                    {
                        string Key = string.Empty;
                        string Field = string.Empty;
                        string Type = string.Empty;
                        if (describeTableModel.KeyValue != null)
                            Key = describeTableModel.KeyValue;
                        if (describeTableModel.FieldValue != null)
                            Field = describeTableModel.FieldValue;
                        if (describeTableModel.TypeValue != null)
                            Type = describeTableModel.TypeValue;

                        if (GetStringDataType().Any(x => Type.Contains(x)))
                        {
                            name = SplitToSpaceLabel(Field);
                            break;
                        }
                    }
                }

                return name;
            }
            public string? GetPrimayKeyTableName(string tableName)
            {
                var referencedTableName = string.Empty;

                using MySqlConnection connection = GetConnection();
                connection.Open();

                // this is generator so nooo need  bind param
                string sql = $@"
                SELECT  COLUMN_NAME
		        FROM 	information_schema.KEY_COLUMN_USAGE
		        WHERE 	table_schema='{DEFAULT_DATABASE}'
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
                    referencedTableName = String.Empty;
                    Debug.WriteLine(ex.Message);
                }
                return referencedTableName;
            }
            public string? GetForeignKeyTableName(string tableName, string field)
            {


                using MySqlConnection connection = GetConnection();
                connection.Open();
                var referencedTableName = string.Empty;
                // this is generator so nooo need  bind param
                string sql = $@"		
            SELECT  REFERENCED_TABLE_NAME
		    FROM 	information_schema.KEY_COLUMN_USAGE
		    WHERE 	table_schema='{DEFAULT_DATABASE}'
		    AND 	TABLE_NAME = '{tableName}'
		    AND     COLUMN_NAME='{field}'
            LIMIT  1 ";
                Console.WriteLine(sql);
                var command = new MySqlCommand(sql, connection);
                try
                {
                    referencedTableName = command.ExecuteScalar().ToString();
                }
                catch (MySqlException ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                return referencedTableName;
            }
            public static string UpperCaseFirst(string? s)
            {
                // Check for empty string.
                if (string.IsNullOrEmpty(s))
                {
                    return string.Empty;
                }
                // Return char and concat substring.
                return char.ToUpper(s[0]) + s[1..];
            }
            public static string LowerCaseFirst(string? s)
            {
                // Check for empty string.
                if (string.IsNullOrEmpty(s))
                {
                    return string.Empty;
                }
                // Return char and concat substring.
                return char.ToLower(s[0]) + s[1..];
            }
            public static string SplitToSpaceLabel(string s)
            {
                if (s.IndexOf(" ") == -1)
                {
                    var r = new Regex(@"
                                (?<=[A-Z])(?=[A-Z][a-z]) |
                                (?<=[^A-Z])(?=[A-Z]) |
                                (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);
                    var t = r.Replace(s, " ").Replace("_", "").Trim().ToLower();
                    string[] splitTableName = t.Split(' ');
                    for (int i = 0; i < splitTableName.Length; i++) // Loop with for.
                    {
                        splitTableName[i] = UpperCaseFirst(splitTableName[i]);
                    }
                    s = string.Join(" ", splitTableName);
                }
                return s;
            }
            public static string SplitToUnderScore(string s)
            {
                var r = new Regex(@"
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
                    string[] splitTableName = t.Split('_');
                    for (int i = 0; i < splitTableName.Length; i++) // Loop with for.
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

