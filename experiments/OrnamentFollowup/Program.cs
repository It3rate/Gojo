using Gojo.Experiments.OrnamentFollowup;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

if (args is ["replay", var inputs, var outputs])
    CampaignCli.Run(["replay", inputs, outputs]);
else if (args is ["verify-source", var auditSpec, var auditPrimary])
    Followup.VerifySource(auditSpec,auditPrimary);
else if (args is ["freeze", var spec, var primary, var report, var destination])
    Followup.Freeze(spec, primary, report, destination);
else if (args.Length is 2 or 3)
{
    var workers = args.Length == 3 ? int.Parse(args[2]) : CampaignTournament.DefaultWorkers;
    Followup.Execute(args[0], args[1], workers);
}
else throw new ArgumentException("Commands: freeze SPEC PRIMARY REPORT RUN | train/lock/assay RUN [workers] | replay INPUT OUTPUT");
