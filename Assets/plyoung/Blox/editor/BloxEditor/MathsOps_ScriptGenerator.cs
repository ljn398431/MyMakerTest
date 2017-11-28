using BloxEngine;
using System;
using System.CodeDom;

namespace BloxEditor
{
	[BloxBlockScriptGenerator(typeof(ADD_Block))]
	[BloxBlockScriptGenerator(typeof(SUB_Block))]
	[BloxBlockScriptGenerator(typeof(MUL_Block))]
	[BloxBlockScriptGenerator(typeof(DIV_Block))]
	[BloxBlockScriptGenerator(typeof(MOD_Block))]
	[BloxBlockScriptGenerator(typeof(B_AND_Block))]
	[BloxBlockScriptGenerator(typeof(B_OR_Block))]
	public class MathsOps_ScriptGenerator : BloxBlockScriptGenerator
	{
		public override CodeExpression CreateBlockCodeExpression(BloxBlockEd bdi)
		{
			CodeExpression obj = (bdi.paramBlocks[0] == null) ? new CodePrimitiveExpression(null) : BloxScriptGenerator.CreateBlockCodeExpression(bdi.paramBlocks[0], null);
			CodeExpression codeExpression = (bdi.paramBlocks[1] == null) ? new CodePrimitiveExpression(null) : BloxScriptGenerator.CreateBlockCodeExpression(bdi.paramBlocks[1], null);
			if (obj == null)
			{
				throw new Exception("error: left expression");
			}
			if (codeExpression == null)
			{
				throw new Exception("error: right expression");
			}
			Type type = bdi.b.GetType();
			CodeBinaryOperatorType op = CodeBinaryOperatorType.Add;
			if (type == typeof(SUB_Block))
			{
				op = CodeBinaryOperatorType.Subtract;
			}
			else if (type == typeof(MUL_Block))
			{
				op = CodeBinaryOperatorType.Multiply;
			}
			else if (type == typeof(DIV_Block))
			{
				op = CodeBinaryOperatorType.Divide;
			}
			else if (type == typeof(MOD_Block))
			{
				op = CodeBinaryOperatorType.Modulus;
			}
			else if (type == typeof(B_AND_Block))
			{
				op = CodeBinaryOperatorType.BitwiseAnd;
			}
			else if (type == typeof(B_OR_Block))
			{
				op = CodeBinaryOperatorType.BitwiseOr;
			}
			return new CodeBinaryOperatorExpression(obj, op, codeExpression);
		}

		public override bool CreateBlockCodeStatements(BloxBlockEd bdi, CodeStatementCollection statements)
		{
			return false;
		}
	}
}
