/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System.Text;

#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
using System.Runtime;
#endif

#if USE_ENTITY_FRAMEWORK_6
namespace System.Data.SQLite.EF6
#else
namespace System.Data.SQLite.Linq
#endif
{
	internal abstract class InternalBase
	{
		// Methods
#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
		protected InternalBase()
		{
		}

		internal abstract void ToCompactString(StringBuilder builder);
		internal virtual string ToFullString()
		{
			StringBuilder builder = new StringBuilder();
			this.ToFullString(builder);
			return builder.ToString();
		}

#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
		[TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
#endif
		internal virtual void ToFullString(StringBuilder builder)
		{
			this.ToCompactString(builder);
		}

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();
			this.ToCompactString(builder);
			return builder.ToString();
		}
	}
}
