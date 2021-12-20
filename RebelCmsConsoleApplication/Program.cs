// See https://aka.ms/new-console-template for more information
using System.Configuration;
using System.Text;
using RebelCmsConsoleApplication.RebelCmsGenerator;

Console.WriteLine("Please try using dotnet run module tablename ");
string appsDll = Environment.GetCommandLineArgs()[0];
string module = Environment.GetCommandLineArgs()[1];
string tableName = Environment.GetCommandLineArgs()[2];
Console.WriteLine("You choose this app run  : " + appsDll);
Console.WriteLine("You choose this module  : " + module);
Console.WriteLine("You choose this table  : " + tableName);

var connectionString = ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString;

CodeGenerator codeGenerator = new(connectionString);
// since we don't have wpf in macos / linux. We create the file . Sorry no choosing ya like wpf
string repository = codeGenerator.GenerateController(tableName, module);
string model = codeGenerator.GenerateController(tableName, module);
string pages = codeGenerator.GenerateController(tableName, module);


var path = "/Users/user/Projects/code/";
var fileNameController = CodeGenerator.GetStringNoUnderScore(tableName,0) + "Controller.cs";
var fileNameModel = CodeGenerator.GetStringNoUnderScore(tableName, 0) + "Model.cs";
var fileNameRepository = CodeGenerator.GetStringNoUnderScore(tableName, 0) + "Repository.cs";
var fileNamePages = CodeGenerator.GetStringNoUnderScore(tableName, 0) + ".cshtml";
try
{
    if (File.Exists(fileNameController))    
        File.Delete(fileNameController);
    
    using FileStream fileStreamController = File.Create(path+"/"+fileNameController);
    fileStreamController.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateController(tableName, module)));

    if (File.Exists(fileNameModel))
        File.Delete(fileNameModel);

    using FileStream fileStreamModel = File.Create(path + "/" + fileNameModel);
    fileStreamModel.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateModel(tableName, module)));

    if (File.Exists(fileNameRepository))
        File.Delete(fileNameRepository);

    using FileStream fileStreamRepository = File.Create(path + "/" + fileNameRepository);
    fileStreamRepository.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateRepository(tableName, module)));

    if (File.Exists(fileNameController))
        File.Delete(fileNameController);

    using FileStream fileStreamPages = File.Create(path + "/" + fileNamePages);
    fileStreamPages.Write(Encoding.UTF8.GetBytes(codeGenerator.GeneratePages(tableName, module)));


}
catch(Exception ex)
{
    Console.WriteLine(ex.ToString());
}

