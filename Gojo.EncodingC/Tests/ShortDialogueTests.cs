using Xunit;
using Xunit.Abstractions;

namespace Gojo.EncodingC.Tests;

public sealed class ShortDialogueTests(ITestOutputHelper output)
{
    [Fact]
    public void WhoseCarSettlesWithTheNarrowClarification() =>
        ClarificationSettlesTheRequestedRole(DialogueQuestion.Owner, DialogueClue.Context, 0, 0, 0);

    [Fact]
    public void WhichCarSettlesWithTheNarrowClarification() =>
        ClarificationSettlesTheRequestedRole(DialogueQuestion.Referent, DialogueClue.Context, 1, 0, 1);

    [Fact]
    public void WhereIsItParkedNeedsTheWiderClarification() =>
        ClarificationSettlesTheRequestedRole(DialogueQuestion.Location, DialogueClue.Identification, 1, 1, 0);

    private void ClarificationSettlesTheRequestedRole(
        DialogueQuestion question, DialogueClue reply, int response, int clarification, int answer)
    {
        var dialogue = ShortDialogue.Create(question);
        Assert.False(dialogue.Read().Debt);
        dialogue.Ask();
        Settle(dialogue, "question");
        Assert.Equal(3UL, dialogue.AnswerSupports);
        Assert.True(dialogue.Read().Ambiguous);
        Assert.False(dialogue.Read().Pressure);
        Assert.Equal(clarification, dialogue.Read().Clarification);
        Assert.Equal(DialogueTurn.Asker, dialogue.Read().Turn);

        dialogue.Observe(reply, response);
        Settle(dialogue, "clarification reply");
        Assert.Equal(1UL << answer, dialogue.AnswerSupports);
        Assert.Equal(1UL << response, dialogue.ReferentSupports);
        Assert.Null(dialogue.Read().Clarification);
        Assert.Equal(answer, dialogue.Read().Offered);
        Assert.True(dialogue.Read().Debt);
        Assert.True(dialogue.Read().Pressure);

        var saved = dialogue.Save();
        output.WriteLine($"Archive with answer pending acknowledgement: {saved.Length} bytes");
        dialogue = ShortDialogue.Restore(saved);
        Assert.Equal(saved, dialogue.Save());
        Assert.Equal(answer, dialogue.Read().Offered);
        Assert.Equal(1UL << answer, dialogue.AnswerSupports);
        Assert.Equal(1UL << response, dialogue.ReferentSupports);
        dialogue.Acknowledge(answer);
        Settle(dialogue, "acknowledgement after restore");
        Assert.Equal(answer, dialogue.Read().Grounded);
        Assert.False(dialogue.Read().Debt);
        Assert.False(dialogue.Read().Pressure);
        Assert.Equal(DialogueTurn.Grounded, dialogue.Read().Turn);
    }

    [Fact]
    public void WhoOwnsItCanSettleWithoutWhichCarItIs()
    {
        var dialogue = ShortDialogue.Create(DialogueQuestion.SharedOwner);
        dialogue.Ask();
        Settle(dialogue, "shared owner question");

        Assert.Equal(3UL, dialogue.ReferentSupports);
        Assert.Equal(1UL, dialogue.AnswerSupports);
        Assert.Equal(0, dialogue.Read().Offered);
        Assert.Null(dialogue.Read().Clarification);
        Assert.False(dialogue.Read().Ambiguous);
        Assert.True(dialogue.Read().Pressure);
        dialogue.Acknowledge(0);
        Settle(dialogue, "shared owner acknowledgement");
        Assert.Equal(0, dialogue.Read().Grounded);
        Assert.Equal(3UL, dialogue.ReferentSupports);
    }

    [Fact]
    public void TheSameClaimsResolveLocallyAndConflictWhenTheQuestionIncludesTheWitness()
    {
        var local = ShortDialogue.Create(DialogueQuestion.Owner);
        var context = ShortDialogue.Create(DialogueQuestion.Owner, DialogueScope.Context);

        foreach (var dialogue in new[] { local, context })
        {
            dialogue.Ask();
            Settle(dialogue, "owner question");
            dialogue.Observe(DialogueClue.Context, 0);
            Settle(dialogue, "local answer");
            Assert.Equal(0, dialogue.Read().Offered);
            dialogue.Observe(DialogueClue.Witness, 1);
        }

        // Keep the same stored utterances and attention when resuming the question.
        local = ShortDialogue.Restore(local.Save());
        context = ShortDialogue.Restore(context.Save());
        local.Acknowledge(0);
        context.Acknowledge(0);
        Settle(local, "local scope with witness present");
        Settle(context, "extended scope with witness present");

        Assert.Equal(1UL, local.AnswerSupports);
        Assert.False(local.Read().Contradiction);
        Assert.Equal(0, local.Read().Grounded);
        Assert.Equal(0UL, context.AnswerSupports);
        Assert.True(context.Read().Contradiction);
        Assert.False(context.Read().Ambiguous);
        Assert.True(context.Read().Debt);
        Assert.True(context.Read().Pressure);
        Assert.Equal(0, context.Read().Offered);
        Assert.Null(context.Read().Clarification);
        Assert.Null(context.Read().Grounded);
        Assert.Equal(DialogueTurn.Respondent, context.Read().Turn);
    }

    [Fact]
    public void TheSharedParkingAreaClueDoesNotSettleExactLocation()
    {
        var dialogue = ShortDialogue.Create(DialogueQuestion.Location);
        dialogue.Ask();
        Settle(dialogue, "location question");
        dialogue.Observe(DialogueClue.Context, 0);
        Settle(dialogue, "shared area");

        Assert.Equal(3UL, dialogue.AnswerSupports);
        Assert.Equal(3UL, dialogue.ReferentSupports);
        Assert.True(dialogue.Read().Ambiguous);
        Assert.Equal(1, dialogue.Read().Clarification);
        Assert.Null(dialogue.Read().Offered);
    }

    [Fact]
    public void ARepairOutsideTheQuestionConeCannotResolveIt()
    {
        var dialogue = ShortDialogue.Create(DialogueQuestion.Location, DialogueScope.Direct);
        dialogue.Ask();
        Settle(dialogue, "direct question");
        Assert.Null(dialogue.Read().Clarification);
        Assert.Equal(DialogueTurn.Respondent, dialogue.Read().Turn);

        dialogue.Observe(DialogueClue.Identification, 1);
        Settle(dialogue, "identification outside scope");

        Assert.Equal(3UL, dialogue.AnswerSupports);
        Assert.True(dialogue.Read().Debt);
        Assert.True(dialogue.Read().Ambiguous);
        Assert.Null(dialogue.Read().Offered);
    }

    private void Settle(ShortDialogue dialogue, string stage)
    {
        for (var round = 1; round <= 12; round++)
            if (dialogue.Step())
            {
                output.WriteLine($"{stage}, quiet in {round} round(s), answer supports {dialogue.AnswerSupports:b2}, {dialogue.Read()}");
                return;
            }
        Assert.Fail($"Dialogue did not settle after {stage}.");
    }
}
