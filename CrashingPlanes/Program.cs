using CsvHelper;
using ILOG.Concert;
using ILOG.CPLEX;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace CrashingPlanes
{
	internal class Program
	{

		public static void Main(string[] args)
		{
			Experiment();
			
		}

		static void Solving()
		{
			var planes = new int[] { 10, 20, 30, 40 };
			var maneuvers = 7;

			foreach (var plane in planes)
			{
				SolvePlanes(plane, maneuvers, "CM");
			}
		}

		static void Experiment()
		{
			var planes = new List<int>();

			for(int i = 10; i <= 45; i += 1)
			{
				planes.Add(i);
			}

			var maneuvers = 7;
			var repeats = 3;
			var measurements = new List<Result>(); 
			var stopwatch = new Stopwatch();

			foreach(var planeCount in planes)
			{
				
				for(int i = 0; i < repeats; i++)
				{
					GenerateRandomInstance(planeCount, maneuvers);
					stopwatch.Restart();
					var status = SolvePlanes(planeCount, maneuvers, "Experiment");
					var elapsed = stopwatch.ElapsedMilliseconds;

					var measurement = new Result
					{
						Planes = planeCount,
						Time = elapsed,
						Solution = status.ToString(),
					};
					measurements.Add(measurement);

				}
			}

			using (var writer = new StreamWriter("measurements.csv"))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(measurements);
			}
		}

		static Cplex.Status SolvePlanes(int planes, int maneuvers, string folder)
		{
			Console.WriteLine($"\nSolving MIP for n={planes} and m={maneuvers}");
			Cplex cplex = new Cplex();
	

			var (var, rng) = PopulateByRow(cplex, planes, maneuvers, folder);

			//cplex.ExportModel($"lpex_{planes}_{maneuvers}.lp");

			Cplex.Status status = null;
			if (cplex.Solve())
			{
				double[] x = cplex.GetValues(var);

				status = cplex.GetStatus();
				cplex.Output().WriteLine("Solution status = " + cplex.GetStatus());
				cplex.Output().WriteLine("Solution value  = " + cplex.ObjValue);

				Console.WriteLine("\nRows - planes.\nColumns - maneuvers");
				for (int plane = 0; plane < planes; plane++)
				{
					Console.Write(plane + ": ");
					for (int maneuver = 0; maneuver < maneuvers; maneuver++)
					{
						var idx = plane * maneuvers + maneuver;
						Console.Write( Math.Abs(x[idx]) + " ");

					}
					Console.WriteLine();
				}

			}
			cplex.End();
			return status;

		}

		static void GenerateRandomInstance(int planes, int maneuvers)
		{
			var random = new Random();
			var sb = new StringBuilder();
			int size = planes * maneuvers;
			var numbers = new int[size * size];

			for(int y = 0; y < size; y++)
			{
				for(int x = 0; x < size; x++)
				{
					var index = y * size + x;
					var planeX = x / maneuvers;
					var planeY = y / maneuvers;

					var propability = random.NextDouble();
					int num = 0;
					if(y == x)
					{
						num = 1;
					}
					else if (y > x)
					{
						var transposedIndex = x * size + y;
						num = numbers[transposedIndex];
					}
					else if(planeX == planeY)
					{
						num = 0;
					}
					else if (propability < 0.065)
					{
						num = 1;
					}

					numbers[index] = num;

				}
				
			}

			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					var index = y * (size) + x;
					var num = numbers[index];
					if (x != size - 1)
					{
						sb.Append(num + " ");
					}
					else
					{
						sb.Append(num);
					}
				}
				if (y != size - 1)
				{
					sb.Append('\n');
				}
			}

					using (var file = new StreamWriter($"Experiment/CM_n={planes}_m={maneuvers}.txt"))
			{
				file.WriteLine(sb.ToString());
			}
		}

		static (INumVar[], IRange[]) PopulateByRow(IMPModeler model, int planes, int maneuvers, string folder)
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

			var matrix = ImportData(folder, planes, maneuvers);

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

			AddOptimization(model, planes, maneuvers, variables);
			//model.AddMinimize();

			return (variables, rng);
		}

		static void AddOptimization(IMPModeler model, int planes, int maneuvers, INumVar[] variables)
		{
			var coefficients = new List<int>();
			for (int plane1 = 0; plane1 < planes; plane1++)
			{
				for (int manuver = 0; manuver < maneuvers; manuver++) {

					coefficients.Add(manuver + 1);
				}
			}

			var minimization = model.ScalProd(variables, coefficients.ToArray());
			model.AddMinimize(minimization);
		}

		static int[][] ImportData(string folder, int planes, int maneuvers)
		{
			var textFile = $"{folder}/CM_n={planes}_m={maneuvers}.txt";
			var matrix = File.ReadAllLines(textFile)
				.Select(line => line.Split(' ')
					.Select(letter => int.Parse(letter)).ToArray()
					).ToArray();

			return matrix;
		}

		public struct Result
		{
			public int Planes { get; set; }
			public long Time { get; set; }
			public string Solution { get; set; }
		}
		
	}

	
}