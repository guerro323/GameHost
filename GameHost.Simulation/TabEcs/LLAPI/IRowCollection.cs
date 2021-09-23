using System;

namespace GameHost.Simulation.TabEcs.LLAPI
{
	/// <summary>
	///     A board contains user-defined columns and rows as an ID format.
	/// </summary>
	public interface IRowCollection<TRow>
    {
	    /// <summary>
	    ///     The maximum row ID of this board.
	    /// </summary>
	    TRow MaxId { get; }

	    /// <summary>
	    ///     How many rows are used.
	    /// </summary>
	    int Count { get; }

	    /// <summary>
	    ///     Currently used rows.
	    /// </summary>
	    Span<TRow> UsedRows { get; }

	    /// <summary>
	    ///     Create a new row, the ID can be a recycled one from <see cref="TrySetUnusedRow" />
	    /// </summary>
	    /// <returns>The row</returns>
	    void CreateRowBulk(Span<TRow> rows);

	    /// <summary>
	    ///     Create a new row, the ID can be a recycled one from <see cref="TrySetUnusedRow" />
	    /// </summary>
	    /// <returns>The row</returns>
	    TRow CreateRow();

	    /// <summary>
	    ///     Set a row to an unused state.
	    /// </summary>
	    /// <param name="row">The row</param>
	    /// <returns>
	    ///     True if this row has been set to unused, if it's false it mean that this row was never registered to the
	    ///     board.
	    /// </returns>
	    bool TrySetUnusedRow(TRow row);

	    /// <summary>
	    ///     Get a column data reference on a row.
	    /// </summary>
	    /// <param name="row">The row</param>
	    /// <param name="arrayOfColumns">An user-defined column array</param>
	    /// <param name="value">The column value</param>
	    /// <typeparam name="TColumn">Column type</typeparam>
	    ref TColumn GetColumn<TColumn>(TRow row, ref TColumn[] arrayOfColumns);
    }
}