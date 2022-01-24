// See https://aka.ms/new-console-template for more information
using System.Configuration;
using System.Text;
using RebelCmsConsoleApplication.RebelCmsGenerator;

Console.WriteLine("Please try using dotnet run module table name ");

var module = string.Empty;
var tableName = string.Empty;
var tableNameDetail = string.Empty;

var count = Environment.GetCommandLineArgs().Length;

var appsDll = Environment.GetCommandLineArgs()[0];


Console.WriteLine("Total arguments : [" + count + "]");
var counter = 0;
foreach(var x in Environment.GetCommandLineArgs())
{
    switch (counter)
    {
        case 1:
            module = CodeGenerator.UpperCaseFirst(Environment.GetCommandLineArgs()[1]);
            break;
        case 2:
            tableName = Environment.GetCommandLineArgs()[2];

            break;

        case 3:
            tableNameDetail = Environment.GetCommandLineArgs()[3];

            break;

    }
    counter++;
}
if (count == 2)
{
    Console.WriteLine("Put one table name at least");
}
if (!string.IsNullOrEmpty(appsDll))
{
    Console.WriteLine("You choose this app run  : " + appsDll);
}
if (string.IsNullOrEmpty(module))
{
    Console.WriteLine("You choose this module  : " + module);
}
if (!string.IsNullOrEmpty(tableName))
{
    Console.WriteLine("You choose this table  : " + tableName);
}
if (!string.IsNullOrEmpty(tableNameDetail))
{
    Console.WriteLine("You choose this detail table  : " + tableNameDetail);
}

CodeGenerator codeGenerator = new(ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString);

const string path = "/Users/user/Projects/code/";
if (string.IsNullOrEmpty(tableNameDetail))
{
    var fileNameController = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Controller.cs";
    var fileNameModel = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Model.cs";
    var fileNameRepository = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Repository.cs";
    var fileNamePages = CodeGenerator.GetStringNoUnderScore(tableName, 1) + ".cshtml";
    try
    {
        if (File.Exists(fileNameController))
            File.Delete(fileNameController);

        using var fileStreamController = File.Create(path + "/" + fileNameController);
        fileStreamController.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateController(module,tableName)));

        if (File.Exists(fileNameModel))
            File.Delete(fileNameModel);

        using var fileStreamModel = File.Create(path + "/" + fileNameModel);
        fileStreamModel.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateModel(module, tableName)));

        if (File.Exists(fileNameRepository))
            File.Delete(fileNameRepository);

        using var fileStreamRepository = File.Create(path + "/" + fileNameRepository);
        fileStreamRepository.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateRepository(module, tableName)));

        if (File.Exists(fileNamePages))
            File.Delete(fileNamePages);


        using var fileStreamPages = File.Create(path + "/" + fileNamePages);
        // there is issue if the field over  5? 
        var data = codeGenerator.GetTableStructure(tableName);
        fileStreamPages.Write(data.Count < 6
            ? Encoding.UTF8.GetBytes(codeGenerator.GeneratePages(module, tableName))
            : Encoding.UTF8.GetBytes(codeGenerator.GeneratePagesFormAndGrid(module, tableName)));
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }

}
else
{
    Console.WriteLine("We generating master detail");
    var fileNameMasterController = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Controller.cs";
    var fileNameMasterModel = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Model.cs";
    var fileNameMasterRepository = CodeGenerator.GetStringNoUnderScore(tableName, 1) + "Repository.cs";

    var fileNameDetailController = CodeGenerator.GetStringNoUnderScore(tableNameDetail, 1) + "Controller.cs";
    var fileNameDetailModel = CodeGenerator.GetStringNoUnderScore(tableNameDetail, 1) + "Model.cs";
    var fileNameDetailRepository = CodeGenerator.GetStringNoUnderScore(tableNameDetail, 1) + "Repository.cs";

    var fileNameMasterDetailPages = CodeGenerator.GetStringNoUnderScore(tableName, 1) + ".cshtml";
    try
    {
        // start controller
        if (File.Exists(fileNameMasterController))
            File.Delete(fileNameMasterController);

        using var fileStreamMasterController = File.Create(path + "/" + fileNameMasterController);
        fileStreamMasterController.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateController(module, tableName,tableNameDetail)));

        if (File.Exists(fileNameDetailController))
            File.Delete(fileNameDetailController);

        using var fileStreamDetailController = File.Create(path + "/" + fileNameDetailController);
        fileStreamDetailController.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateController(module, tableNameDetail)));
        // end controller

        // start model 
        if (File.Exists(fileNameMasterModel))
            File.Delete(fileNameMasterModel);

        using var fileStreamMasterModel = File.Create(path + "/" + fileNameMasterModel);
        fileStreamMasterModel.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateModel(module,tableName,true)));

        if (File.Exists(fileNameDetailModel))
            File.Delete(fileNameDetailModel);

        using var fileStreamDetailModel = File.Create(path + "/" + fileNameDetailModel);
        fileStreamDetailModel.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateModel(module,tableNameDetail)));
        // end model

        // start repository
        if (File.Exists(fileNameMasterRepository))
            File.Delete(fileNameMasterRepository);

        using var fileStreamMasterRepository = File.Create(path + "/" + fileNameMasterRepository);
        fileStreamMasterRepository.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateRepository(module,tableName,tableNameDetail,true)));

        if (File.Exists(fileNameDetailRepository))
            File.Delete(fileNameDetailRepository);

        using var fileStreamDetailRepository = File.Create(path + "/" + fileNameDetailRepository);
        fileStreamDetailRepository.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateRepository(module,tableNameDetail)));

        // end detail repository

        // start pages
        if (File.Exists(fileNameMasterDetailPages))
            File.Delete(fileNameMasterDetailPages);

        using var fileStreamPages = File.Create(path + "/" + fileNameMasterDetailPages);
        fileStreamPages.Write(Encoding.UTF8.GetBytes(codeGenerator.GenerateMasterAndDetail(module,tableName,tableNameDetail)));
        // thinking what if the person just want to key in only ?
        // so we need optional form only . later
        Console.WriteLine("Check your master detail code");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }


}