// See https://aka.ms/new-console-template for more information
using System.Configuration;
using System.Text;
using RebelCmsConsoleApplication.RebelCmsGenerator;

Console.WriteLine("Please try using dotnet run module tablename ");
string appsDll = Environment.GetCommandLineArgs()[0];
string module = CodeGenerator.UpperCaseFirst(Environment.GetCommandLineArgs()[1]);
string tableName = Environment.GetCommandLineArgs()[2];
string detailTableName = Environment.GetCommandLineArgs()[3];
Console.WriteLine("You choose this app run  : " + appsDll);
Console.WriteLine("You choose this module  : " + module);
Console.WriteLine("You choose this table  : " + tableName);
// what happen if they want master detail ? So if existed the third we generated master and detail and a bit complex
// compare single page row in edit 


var connectionString = ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString;

CodeGenerator codeGenerator = new(connectionString);
// mariadb utf3 error even thou we don't have utf3 only utf8
codeGenerator.SetByPassMariaDbError();

// since we don't have wpf in macos / linux. We create the file . Sorry no choosing ya like wpf
string repository = codeGenerator.GenerateController(tableName, module);
string model = codeGenerator.GenerateController(tableName, module);
string pages = codeGenerator.GenerateController(tableName, module);


var path = "/Users/user/Projects/code/";
if (detailTableName == null)
{
    var fileNameController = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Controller.cs";
    var fileNameModel = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Model.cs";
    var fileNameRepository = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Repository.cs";
    var fileNamePages = CodeGenerator.GetStringNoUnderScore(tableName, 1) + ".cshtml";
    try
    {
        if (File.Exists(fileNameController))
            File.Delete(fileNameController);

        using FileStream fileStreamController = File.Create(path + "/" + fileNameController);
        fileStreamController.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateController(tableName, module)));

        if (File.Exists(fileNameModel))
            File.Delete(fileNameModel);

        using FileStream fileStreamModel = File.Create(path + "/" + fileNameModel);
        fileStreamModel.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateModel(tableName, module)));

        if (File.Exists(fileNameRepository))
            File.Delete(fileNameRepository);

        using FileStream fileStreamRepository = File.Create(path + "/" + fileNameRepository);
        fileStreamRepository.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateRepository(tableName, module)));

        if (File.Exists(fileNamePages))
            File.Delete(fileNamePages);


        using FileStream fileStreamPages = File.Create(path + "/" + fileNamePages);
        // there is issue if the field over  5? is not suitabler soooo  we check first
        var data = codeGenerator.GetTableStructure(tableName);
        if (data.Count < 6)
        {
            fileStreamPages.Write(Encoding.UTF8.GetBytes(codeGenerator.GeneratePages(tableName, module)));
        }
        else
        {

            fileStreamPages.Write(Encoding.UTF8.GetBytes(codeGenerator.GeneratePagesFormAndGrid(tableName, module)));
        }


    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }

}
else
{
    var fileNameMasterController = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Controller.cs";
    var fileNameMasterModel = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Model.cs";
    var fileNameMasterRepository = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Repository.cs";

    var fileNameDetailController = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Controller.cs";
    var fileNameDetailModel = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Model.cs";
    var fileNameDetailRepository = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Repository.cs";

    var fileNameMasterDetailPages = CodeGenerator.GetStringNoUnderScore(tableName, 1) + ".cshtml";
    try
    {
        if (File.Exists(fileNameMasterController))
            File.Delete(fileNameMasterController);

        using FileStream fileStreamMasterController = File.Create(path + "/" + fileNameMasterController);
        fileStreamMasterController.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateController(tableName, module)));

        if (File.Exists(fileNameDetailController))
            File.Delete(fileNameDetailController);

        using FileStream fileStreamDetailController = File.Create(path + "/" + fileNameMasterController);
        fileStreamDetailController.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateController(detailTableName, module, tableName)));


        if (File.Exists(fileNameMasterModel))
            File.Delete(fileNameMasterModel);

        using FileStream fileStreamMasterModel = File.Create(path + "/" + fileNameMasterModel);
        fileStreamMasterModel.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateModel(tableName, module)));

        if (File.Exists(fileNameDetailModel))
            File.Delete(fileNameDetailModel);

        using FileStream fileStreamDetailModel = File.Create(path + "/" + fileNameDetailModel);
        fileStreamDetailModel.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateModel(detailTableName, module)));


        if (File.Exists(fileNameMasterRepository))
            File.Delete(fileNameMasterRepository);

        using FileStream fileStreamMasterRepository = File.Create(path + "/" + fileNameMasterRepository);
        fileStreamMasterRepository.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateRepository(tableName, module)));

        if (File.Exists(fileNameDetailRepository))
            File.Delete(fileNameDetailRepository);

        using FileStream fileStreamDetailRepository = File.Create(path + "/" + fileNameDetailRepository);
        fileStreamDetailRepository.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateRepository(detailTableName, module, tableName)));


        if (File.Exists(fileNameMasterDetailPages))
            File.Delete(fileNameMasterDetailPages);

        using FileStream fileStreamPages = File.Create(path + "/" + fileNameMasterDetailPages);
        fileStreamPages.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateMasterDetailPages(tableName, detailTableName, module)));

        // there is another one .. for listing purpose  
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }


}