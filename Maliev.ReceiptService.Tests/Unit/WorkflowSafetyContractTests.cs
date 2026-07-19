namespace Maliev.ReceiptService.Tests.Unit;

/// <summary>
/// Guards protected-branch workflows against coupling validation to deployment.
/// </summary>
public sealed class WorkflowSafetyContractTests
{
    /// <summary>
    /// Verifies that protected-branch workflows call the read-only validation workflow only.
    /// </summary>
    /// <param name="workflowFile">The workflow file to inspect.</param>
    [Theory]
    [InlineData("ci-develop.yml")]
    [InlineData("ci-main.yml")]
    [InlineData("ci-staging.yml")]
    public void ProtectedBranchWorkflow_IsValidationOnly(string workflowFile)
    {
        var yaml = ReadWorkflow(workflowFile);

        Assert.Contains("permissions:\n  contents: read", yaml, StringComparison.Ordinal);
        Assert.Contains("uses: ./.github/workflows/_build-and-test.yml", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("google-github-actions", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker push", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("maliev-gitops", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GCP_SA_KEY", yaml, StringComparison.Ordinal);
        Assert.DoesNotContain("setup-kustomize", yaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gh pr create", yaml, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadWorkflow(string workflowFile)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var workflowPath = Path.Combine(directory.FullName, ".github", "workflows", workflowFile);
            if (File.Exists(workflowPath))
            {
                return File.ReadAllText(workflowPath).Replace("\r\n", "\n", StringComparison.Ordinal);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate .github/workflows/{workflowFile}.");
    }
}
