using BloxEngine;
using System.CodeDom;
using System.Collections;

namespace BloxEditor
{
	[BloxBlockScriptGenerator(typeof(List_Add_Block))]
	public class List_Add_ScriptGenerator : BloxBlockScriptGenerator
	{
		public override CodeExpression CreateBlockCodeExpression(BloxBlockEd bdi)
		{
			return null;
		}

		public override bool CreateBlockCodeStatements(BloxBlockEd bdi, CodeStatementCollection statements)
		{
			CodeExpression codeExpression = BloxScriptGenerator.CreateBlockCodeExpression(bdi.contextBlock, typeof(IList));
			if (codeExpression == null)
			{
				return false;
			}
			CodeExpression codeExpression2 = BloxScriptGenerator.CreateBlockCodeExpression(bdi.paramBlocks[0], null) ?? new CodePrimitiveExpression(null);
			CodeExpression expression = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(BloxUtil)), "ListAdd", codeExpression, codeExpression2);
			statements.Add(new CodeExpressionStatement(expression));
			return true;
		}
	}
}
