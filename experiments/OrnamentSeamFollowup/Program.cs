using Gojo.Experiments.OrnamentSeamFollowup;

if (args is ["check"])
{
    SeamSearch.CheckOnSyntheticRing();
    return 0;
}
if (args.Length < 3)
{
    Console.Error.WriteLine("check | freeze RUN PRIMARY SPEC | train/lock/assay RUN PRIMARY [WORKERS]");
    return 2;
}
var command = args[0];
var folder = Path.GetFullPath(args[1]);
var primary = Path.GetFullPath(args[2]);
if (command == "freeze")
{
    if (args.Length != 4) throw new ArgumentException("Supply a prospective follow-up protocol file.");
    SeamSearch.Freeze(folder,primary,Path.GetFullPath(args[3]));
}
else
{
    int workers = args.Length >= 4 ? int.Parse(args[3],System.Globalization.CultureInfo.InvariantCulture) :
        Math.Clamp(Environment.ProcessorCount-2,1,8);
    SeamSearch.Execute(command,folder,primary,workers);
}
return 0;
