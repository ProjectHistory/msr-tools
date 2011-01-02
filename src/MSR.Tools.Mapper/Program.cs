﻿/*
 * MSR Tools - tools for mining software repositories
 * 
 * Copyright (C) 2010-2011  Semyon Kirnosenko
 */

using System;

namespace MSR.Tools.Mapper
{
	class Program
	{
		static void Main(string[] args)
		{
			string configFile;
			string cmd;
			bool createSchema = false;
			int numberOfRevisions = 0;
			
			try
			{
				configFile = args[0];
				cmd = args[1];
				int i = 2;
				while (i < args.Length)
				{
					switch (args[i])
					{
						case "-c":
							createSchema = true;
							break;
						case "-n":
							i++;
							numberOfRevisions = Convert.ToInt32(args[i]);
							break;
						default:
							break;
					}
					i++;
				}
			}
			catch
			{
				Console.WriteLine("usage: MSR.Tools.Mapper CONFIG_FILE_NAME COMMAND [ARGS]");
				Console.WriteLine("Commands:");
				Console.WriteLine("  map		map data from software repositories to database");
				Console.WriteLine("    -c		create data base");
				Console.WriteLine("    -n N		map commits from first to N incrementally");
				Console.WriteLine("  check		check validity of mapped data");
				Console.WriteLine("    -n N		check data till revision N");
				
				return;
			}
			
			MappingTool mapper = null;
			try
			{
				mapper = new MappingTool(configFile);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
			
			switch (cmd)
			{
				case "map":
					mapper.Map(createSchema, numberOfRevisions);
					break;
				case "check":
					mapper.Check(numberOfRevisions);
					break;
				default:
					Console.WriteLine("Unknown command {0}", cmd);
					break;
			}
		}
	}
}
