using HYDRON.Validator;
using Xunit;

namespace HYDRON.Tests;

public class VoteAggregatorTests
{
    [Fact]
    public void Constructor_EmptyFirstValidator_Throws()
        => Assert.Throws<ArgumentException>(() => new VoteAggregator("", 3));

    [Fact]
    public void Constructor_ZeroTotalAssigned_Throws()
        => Assert.Throws<ArgumentException>(() => new VoteAggregator("v1", 0));

    [Fact]
    public void Constructor_NegativeTotalAssigned_Throws()
        => Assert.Throws<ArgumentException>(() => new VoteAggregator("v1", -1));

    [Fact]
    public void Constructor_ValidArgs_ResultIsPending()
        => Assert.Equal(ConsensusResult.Pending, new VoteAggregator("v1", 3).Result);

    [Fact]
    public void SubmitVote_FirstValidatorRejects_InstantVeto()
    {
        VoteAggregator agg = new VoteAggregator("v1", 5);
        agg.SubmitVote("v1", false);
        Assert.Equal(ConsensusResult.VetoedByFirstValidator, agg.Result);
    }

    [Fact]
    public void SubmitVote_FirstValidatorApproves_NotVetoed()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        Assert.Equal(ConsensusResult.Pending, agg.Result);
    }

    [Fact]
    public void SubmitVote_NonFirstValidatorRejects_NoVeto()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v2", false);
        Assert.Equal(ConsensusResult.Pending, agg.Result);
    }

    [Fact]
    public void SubmitVote_FirstValidatorAddressCaseInsensitive_Vetoes()
    {
        VoteAggregator agg = new VoteAggregator("VALIDATOR1", 3);
        agg.SubmitVote("validator1", false);
        Assert.Equal(ConsensusResult.VetoedByFirstValidator, agg.Result);
    }

    [Fact]
    public void SubmitVote_SupermajorityApproval_3of3_Approved()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", true);
        agg.SubmitVote("v3", true);
        Assert.Equal(ConsensusResult.Approved, agg.Result);
    }

    [Fact]
    public void SubmitVote_SupermajorityApproval_2of3_Approved()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", true);
        Assert.Equal(ConsensusResult.Approved, agg.Result);
    }

    [Fact]
    public void SubmitVote_BelowSupermajorityApproval_1of3_StillPending()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        Assert.Equal(ConsensusResult.Pending, agg.Result);
    }

    [Fact]
    public void SubmitVote_SupermajorityApproval_4of5_Approved()
    {
        VoteAggregator agg = new VoteAggregator("v1", 5);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", true);
        agg.SubmitVote("v3", true);
        agg.SubmitVote("v4", true);
        Assert.Equal(ConsensusResult.Approved, agg.Result);
    }

    [Fact]
    public void SubmitVote_BelowSupermajorityApproval_3of5_StillPending()
    {
        VoteAggregator agg = new VoteAggregator("v1", 5);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", true);
        agg.SubmitVote("v3", true);
        Assert.Equal(ConsensusResult.Pending, agg.Result);
    }

    [Fact]
    public void SubmitVote_SupermajorityRejection_2of3_Rejected()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true); // first validator approves — no veto
        agg.SubmitVote("v2", false);
        agg.SubmitVote("v3", false);
        Assert.Equal(ConsensusResult.Rejected, agg.Result);
    }

    [Fact]
    public void SubmitVote_BelowSupermajorityRejection_1of3_StillPending()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", false);
        Assert.Equal(ConsensusResult.Pending, agg.Result);
    }

    [Fact]
    public void SubmitVote_DuplicateVoter_Throws()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v2", true);
        Assert.Throws<InvalidOperationException>(() => agg.SubmitVote("v2", false));
    }

    [Fact]
    public void SubmitVote_DuplicateVoterCaseInsensitive_Throws()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("V2", true);
        Assert.Throws<InvalidOperationException>(() => agg.SubmitVote("v2", true));
    }

    // --- Vote after result ---

    [Fact]
    public void SubmitVote_AfterVeto_Throws()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", false);
        Assert.Throws<InvalidOperationException>(() => agg.SubmitVote("v2", true));
    }

    [Fact]
    public void SubmitVote_AfterApproval_Throws()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", true);
        Assert.Throws<InvalidOperationException>(() => agg.SubmitVote("v3", true));
    }

    [Fact]
    public void SubmitVote_EmptyAddress_Throws()
        => Assert.Throws<ArgumentException>(() => new VoteAggregator("v1", 3).SubmitVote("", true));

    [Fact]
    public void Approvers_ContainsOnlyApprovers()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", false);
        Assert.Contains("v1", agg.Approvers);
        Assert.DoesNotContain("v2", agg.Approvers);
    }

    [Fact]
    public void Rejecters_ContainsOnlyRejecters()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", false);
        Assert.Contains("v2", agg.Rejecters);
        Assert.DoesNotContain("v1", agg.Rejecters);
    }

    [Fact]
    public void VotesCast_TracksCorrectly()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        Assert.Equal(0, agg.VotesCast);
        agg.SubmitVote("v1", true);
        Assert.Equal(1, agg.VotesCast);
        agg.SubmitVote("v2", false);
        Assert.Equal(2, agg.VotesCast);
    }

    [Fact]
    public void AllVoted_TrueWhenAllHaveVoted()
    {
        VoteAggregator agg = new VoteAggregator("v1", 2);
        agg.SubmitVote("v1", true);
        agg.SubmitVote("v2", true);
        Assert.True(agg.AllVoted);
    }

    [Fact]
    public void AllVoted_FalseBeforeAllHaveVoted()
    {
        VoteAggregator agg = new VoteAggregator("v1", 3);
        agg.SubmitVote("v1", true);
        Assert.False(agg.AllVoted);
    }
}
