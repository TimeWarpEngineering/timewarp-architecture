namespace TimeWarp.Architecture.Template.Tests;
using TimeWarp.Fixie;
using Boxed.DotnetNewTest;
using Boxed.Templates.FunctionalTest;

public class TestingConvention : TimeWarp.Fixie.TestingConvention { }

public class TemplateTest
{
  private const string ShortName = "timewarp-architecture";
  private const string solutionName = "TimeWarp.Architecture.sln";
  private static readonly string[] DefaultArguments = new string[]
  {
  };
  private static readonly TempDirectory TempDirectoryName;

  /// <summary>
  /// 
  //Template options:
  //-p, --process Include the TimeWarp Process Documents, an editable minimalist process based in markdown documents
  //                   Type: bool
  //                   Default: true
  //-g, --grpc Include the grpc container app(Superheros)
  //                   Type: bool
  //                   Default: true
  //-ap, --api Include the api container app(Weather)
  //                   Type: bool
  //                   Default: true
  //-w, --web Include the web container app
  //                   Type: bool
  //                   Default: true
  //-y, --yarp Include the yarp proxy server
  //                   Type: bool
  //                   Default: true
  //-c, --cosmosdb Add CosmosDb Features
  //                   Type: bool
  //                   Default: true
  //-co, --counter Add Counter Features to Web.Spa
  //                   Type: bool
  //                   Default: true
  //-e, --eventstream Eventstream is an example of how to implement middleware in Web.Spa
  //                   Type: bool
  //                   Default: true
  /// </summary>
  /// <param name="name"></param>
  /// <param name="arguments"></param>
  /// <returns></returns>

  [Input( "Test_0", new string[] { "allow-scripts=yes" } )]
  [Input( "Test_1", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_2", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_3", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_4", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_5", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_6", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_7", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_8", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_9", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_10", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_11", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_12", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_13", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_14", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_15", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_16", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_17", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_18", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_19", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_20", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_21", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_22", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_23", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_24", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_25", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_26", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_27", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_28", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_29", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_30", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_31", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_32", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_33", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_34", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_35", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_36", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_37", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_38", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_39", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_40", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_41", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_42", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_43", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_44", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_45", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_46", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_47", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_48", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_49", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_50", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_51", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_52", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_53", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_54", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_55", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_56", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_57", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_58", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_59", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_60", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_61", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_62", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_63", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_64", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_65", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_66", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_67", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_68", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_69", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_70", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_71", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_72", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_73", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_74", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_75", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_76", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_77", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_78", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_79", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_80", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_81", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_82", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_83", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_84", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_85", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_86", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_87", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_88", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_89", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_90", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_91", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_92", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_93", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_94", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_95", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_96", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_97", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_98", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_99", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_100", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_101", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_102", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_103", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_104", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_105", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_106", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_107", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_108", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_109", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_110", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_111", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_112", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_113", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_114", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_115", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_116", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_117", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_118", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_119", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_120", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_121", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_122", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_123", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_124", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_125", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_126", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_127", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_128", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=true", "allow-scripts=yes" } )]
  [Input( "Test_129", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_130", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_131", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_132", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_133", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_134", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_135", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_136", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_137", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_138", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_139", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_140", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_141", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_142", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_143", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_144", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_145", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_146", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_147", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_148", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_149", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_150", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_151", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_152", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_153", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_154", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_155", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_156", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_157", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_158", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_159", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_160", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_161", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_162", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_163", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_164", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_165", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_166", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_167", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_168", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_169", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_170", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_171", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_172", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_173", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_174", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_175", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_176", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_177", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_178", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_179", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_180", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_181", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_182", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_183", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_184", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_185", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_186", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_187", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_188", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_189", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_190", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_191", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_192", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=true", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_193", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_194", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_195", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_196", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_197", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_198", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_199", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_200", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_201", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_202", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_203", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_204", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_205", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_206", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_207", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_208", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_209", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_210", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_211", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_212", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_213", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_214", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_215", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_216", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_217", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_218", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_219", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_220", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_221", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_222", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_223", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_224", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=true", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_225", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_226", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_227", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_228", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_229", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_230", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_231", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_232", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_233", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_234", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_235", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_236", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_237", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_238", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_239", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_240", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=true", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_241", new string[] { "process=true", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_242", new string[] { "process=false", "grpc=true", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_243", new string[] { "process=true", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_244", new string[] { "process=false", "grpc=false", "api=true", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_245", new string[] { "process=true", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_246", new string[] { "process=false", "grpc=true", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_247", new string[] { "process=true", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_248", new string[] { "process=false", "grpc=false", "api=false", "web=true", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_249", new string[] { "process=true", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_250", new string[] { "process=false", "grpc=true", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_251", new string[] { "process=true", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_252", new string[] { "process=false", "grpc=false", "api=true", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_253", new string[] { "process=true", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_254", new string[] { "process=false", "grpc=true", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_255", new string[] { "process=true", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  [Input( "Test_256", new string[] { "process=false", "grpc=false", "api=false", "web=false", "yarp=false", "cosmosdb=false", "counter=false", "eventstream=false", "allow-scripts=yes" } )]
  public async Task RestoreAndBuild_CustomArguments_IsSuccessful(string name, params string[] arguments)
  {
    var tempDirectory = TempDirectory.NewTempDirectory();

    var project = await tempDirectory
        .DotnetNewAsync( ShortName, name, DefaultArguments.ToArguments( arguments ) )
        .ConfigureAwait( false );
    await project.DotnetRestoreWithRetryAsync().ConfigureAwait(false);
    //await project.DotnetBuildAsync().ConfigureAwait( false );
    if (project.DotnetBuildAsync().IsCompletedSuccessfully)
    {
      Directory.Delete( project.DirectoryPath );
    }
  }

  private static Task InstallTemplateAsync() => DotnetNew.InstallAsync<TemplateTest>( solutionName );

  private static Task UnInstallTemplateAsync()
  {
    return DotnetNew.UninstallAsync(solutionName);
  }

  public async static Task Setup() =>  await InstallTemplateAsync();
  public async static Task Cleanup() => await UnInstallTemplateAsync();
}