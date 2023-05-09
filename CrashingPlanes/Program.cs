using ILOG.Concert;
using ILOG.CPLEX;
using System.Numerics;

namespace CrashingPlanes
{
	internal class Program
	{

		public static void Main(string[] args)
		{
			var planes = 10;
			var maneuvers = 7;

			Cplex cplex = new Cplex();

			var (var, rng) = PopulateByRow(cplex, planes, maneuvers);

			cplex.ExportModel("lpex1.lp");

			if (cplex.Solve())
			{
				double[] x = cplex.GetValues(var);

				cplex.Output().WriteLine("Solution status = " + cplex.GetStatus());
				//cplex.Output().WriteLine("Solution value  = " + cplex.ObjValue);

				Console.WriteLine("\nRows - planes.\nColumns - maneuvers");
				
				for(int plane = 0; plane < planes; plane++)
				{
					Console.Write(plane + ": ");
					for(int maneuver = 0; maneuver < maneuvers; maneuver++)
					{
						var idx = plane * maneuvers + maneuver;
						Console.Write(x[idx] + " ");

					}
					Console.WriteLine();
				}

			}
			cplex.End();
			
		}

		static (INumVar[], IRange[]) PopulateByRow(IMPModeler model, int planes, int maneuvers)
		{
			var variableNames = new List<string>(maneuvers * planes);
			for (int i = 0; i < planes; i++)
			{
				for (int j = 0; j < maneuvers; j++)
				{
					variableNames.Add($"samolot_{i}_manewr_{j}");
				}
			}

			var variables = model.NumVarArray(planes * maneuvers, 0, 1, NumVarType.Int, variableNames.ToArray());

			var singlePlaneConstraints = new List<INumExpr>();
			for(int plane = 0; plane < planes; plane++)
			{
				var plane_sum = new List<INumExpr>();
				for(int maneuver = 0; maneuver < maneuvers; maneuver++)
				{
					var idx = plane * maneuvers + maneuver;
					plane_sum.Add(variables[idx]);
				}
				singlePlaneConstraints.Add(model.Sum(plane_sum.ToArray()));
			}

			var matrix = ImportData(planes, maneuvers);

			var collisionConstraints = new List<INumExpr>();
			for(int plane1 = 0; plane1 < planes; plane1++)
			{
				for(int manuver1 = 0; manuver1 < maneuvers; manuver1++)
				{
					for (int plane2 = plane1 + 1; plane2 < planes; plane2++)
					{
						for (int manuver2 = 0; manuver2 < maneuvers; manuver2++)
						{
							var plane1idx = plane1 * maneuvers + manuver1;
							var plane2idx = plane2 * maneuvers + manuver2;

							if (matrix[plane1idx][plane2idx] == 1)
							{
								collisionConstraints.Add(model.Sum(variables[plane1idx], variables[plane2idx]));
							}
						}

					}
				}

			}


			var rng = new IRange[singlePlaneConstraints.Count + collisionConstraints.Count];
			for(int i = 0; i < singlePlaneConstraints.Count; i++)
			{
				rng[i] = model.AddEq(singlePlaneConstraints[i], 1, $"sp_{i}");
			}

			for (int i = 0; i < collisionConstraints.Count; i++)
			{
				rng[i] = model.AddLe(collisionConstraints[i], 1, $"col_{i}");
			}


			model.AddMinimize();
			return (variables, rng);
		}

		static int[][] ImportData(int planes, int maneuvers)
		{
			var textFile = $"CM/CM_n={planes}_m={maneuvers}.txt";
			var matrix = File.ReadAllLines(textFile)
				.Select(line => line.Split(' ')
					.Select(letter => int.Parse(letter)).ToArray()
					).ToArray();

			return matrix;
		}
		
	}

	
}