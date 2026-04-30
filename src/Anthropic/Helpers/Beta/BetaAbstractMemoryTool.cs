using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.Models.Beta.Messages;

namespace Anthropic.Helpers.Beta;

/// <summary>
/// Abstract base class for memory tool implementations.
///
/// <para>Subclass this to provide a custom memory backend (filesystem, database, cloud
/// storage, encrypted store, etc.) that plugs into <see cref="BetaToolRunner"/>. The
/// helper handles the tool definition and command parsing; subclasses implement the
/// six storage operations.</para>
///
/// <para>Each abstract method receives the typed command object for one of the memory
/// commands and returns a tool result (a string or a list of content blocks).</para>
///
/// <para>Example:</para>
/// <code>
/// public class MyMemoryTool : BetaAbstractMemoryTool
/// {
///     protected override Task&lt;BetaToolResultBlockParamContent&gt; ViewAsync(
///         BetaMemoryTool20250818ViewCommand command, CancellationToken ct) =&gt;
///         Task.FromResult&lt;BetaToolResultBlockParamContent&gt;("file contents");
///
///     // ... implement the other five methods
/// }
///
/// var client = new AnthropicClient();
/// var memory = new MyMemoryTool();
/// var runner = client.Beta.Messages.ToolRunner(parameters, [memory]);
/// var result = await runner.RunUntilDoneAsync();
/// </code>
/// </summary>
public abstract class BetaAbstractMemoryTool : IBetaRunnableTool
{
    private readonly BetaCacheControlEphemeral? _cacheControl;

    /// <summary>
    /// Creates a new memory tool helper.
    /// </summary>
    /// <param name="cacheControl">
    /// Optional cache breakpoint to apply to the tool definition.
    /// </param>
    protected BetaAbstractMemoryTool(BetaCacheControlEphemeral? cacheControl = null)
    {
        _cacheControl = cacheControl;
    }

    /// <inheritdoc />
    public string Name => "memory";

    /// <inheritdoc />
    public BetaToolUnion Definition => new BetaMemoryTool20250818 { CacheControl = _cacheControl };

    /// <inheritdoc />
    public Task<BetaToolResultBlockParamContent> ExecuteAsync(
        BetaToolUseBlock toolUseBlock,
        CancellationToken cancellationToken
    )
    {
        var element = JsonSerializer.SerializeToElement(toolUseBlock.Input);
        var command = JsonSerializer.Deserialize<BetaMemoryTool20250818Command>(element)!;
        return ExecuteAsync(command, cancellationToken);
    }

    /// <summary>
    /// Dispatches a parsed memory command to the appropriate handler method based on
    /// the command discriminator. Subclasses typically don't override this.
    /// </summary>
    public virtual Task<BetaToolResultBlockParamContent> ExecuteAsync(
        BetaMemoryTool20250818Command command,
        CancellationToken cancellationToken
    )
    {
        return command.Match(
            tool20250818View: cmd => ViewAsync(cmd, cancellationToken),
            tool20250818Create: cmd => CreateAsync(cmd, cancellationToken),
            tool20250818StrReplace: cmd => StrReplaceAsync(cmd, cancellationToken),
            tool20250818Insert: cmd => InsertAsync(cmd, cancellationToken),
            tool20250818Delete: cmd => DeleteAsync(cmd, cancellationToken),
            tool20250818Rename: cmd => RenameAsync(cmd, cancellationToken)
        );
    }

    /// <summary>
    /// View the contents of a memory path (directory listing or file contents).
    /// </summary>
    protected abstract Task<BetaToolResultBlockParamContent> ViewAsync(
        BetaMemoryTool20250818ViewCommand command,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Create a new memory file with the specified content.
    /// </summary>
    protected abstract Task<BetaToolResultBlockParamContent> CreateAsync(
        BetaMemoryTool20250818CreateCommand command,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Replace text in a memory file.
    /// </summary>
    protected abstract Task<BetaToolResultBlockParamContent> StrReplaceAsync(
        BetaMemoryTool20250818StrReplaceCommand command,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Insert text at a specific line in a memory file.
    /// </summary>
    protected abstract Task<BetaToolResultBlockParamContent> InsertAsync(
        BetaMemoryTool20250818InsertCommand command,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Delete a memory file or directory.
    /// </summary>
    protected abstract Task<BetaToolResultBlockParamContent> DeleteAsync(
        BetaMemoryTool20250818DeleteCommand command,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Rename or move a memory file or directory.
    /// </summary>
    protected abstract Task<BetaToolResultBlockParamContent> RenameAsync(
        BetaMemoryTool20250818RenameCommand command,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Optional bulk-clear operation for backends that support it. The default
    /// implementation throws <see cref="System.NotImplementedException"/>.
    /// </summary>
    public virtual Task<BetaToolResultBlockParamContent> ClearAllMemoryAsync(
        CancellationToken cancellationToken = default
    )
    {
        throw new System.NotImplementedException("ClearAllMemoryAsync not implemented");
    }
}
