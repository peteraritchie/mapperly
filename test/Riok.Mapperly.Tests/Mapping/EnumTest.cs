using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Diagnostics;

namespace Riok.Mapperly.Tests.Mapping;

[UsesVerify]
public class EnumTest
{
    [Fact]
    public void EnumToOtherEnumWithExplicitEnumMapping()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnumValue(E2.e, E1.E), MapEnum(EnumMappingStrategy.ByName)] public partial E1 ToE1(E2 source);",
            "public enum E1 {A, B, C, D, E, f, F}",
            "public enum E2 {A = 100, B, C, d, e, E, f}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowInfoDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E2.A => global::E1.A,
                    global::E2.B => global::E1.B,
                    global::E2.C => global::E1.C,
                    global::E2.e => global::E1.E,
                    global::E2.E => global::E1.E,
                    global::E2.f => global::E1.f,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E2 is not supported"),
                };
                """
            )
            .HaveDiagnostic(new(DiagnosticDescriptors.SourceEnumValueNotMapped, "Enum member d (103) on E2 not found on target enum E1"))
            .HaveDiagnostic(new(DiagnosticDescriptors.TargetEnumValueNotMapped, "Enum member D (3) on E1 not found on source enum E2"));
    }

    [Fact]
    public void EnumToOtherEnumWithExplicitEnumMappingTargetDuplicated()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnumValue(E2.e, E1.E), MapEnumValue(E2.d, E1.E), MapEnum(EnumMappingStrategy.ByName)] public partial E1 ToE1(E2 source);",
            "public enum E1 {A, B, C, D, E, f, F}",
            "public enum E2 {A = 100, B, C, d, e, E, f}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowInfoDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E2.A => global::E1.A,
                    global::E2.B => global::E1.B,
                    global::E2.C => global::E1.C,
                    global::E2.d => global::E1.E,
                    global::E2.e => global::E1.E,
                    global::E2.E => global::E1.E,
                    global::E2.f => global::E1.f,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E2 is not supported"),
                };
                """
            )
            .HaveDiagnostic(new(DiagnosticDescriptors.TargetEnumValueNotMapped, "Enum member D (3) on E1 not found on source enum E2"));
    }

    [Fact]
    public void EnumToOtherEnumWithExplicitEnumMappingDuplicateSource()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnumValue(E2.e, E1.E), MapEnumValue(E2.e, E1.F), MapEnum(EnumMappingStrategy.ByName)] public partial E1 ToE1(E2 source);",
            "public enum E1 {A, B, C, D, E, f, F}",
            "public enum E2 {A = 100, B, C, d, e, E, f}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E2.A => global::E1.A,
                    global::E2.B => global::E1.B,
                    global::E2.C => global::E1.C,
                    global::E2.e => global::E1.E,
                    global::E2.E => global::E1.E,
                    global::E2.f => global::E1.f,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E2 is not supported"),
                };
                """
            )
            .HaveDiagnostic(
                new(
                    DiagnosticDescriptors.EnumSourceValueDuplicated,
                    "Enum source value E2.e is specified multiple times, a source enum value may only be specified once"
                )
            );
    }

    [Fact]
    public void MapEnumValueEnumTypeTargetMismatch()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnumValue(E2.A, E2.A), MapEnum(EnumMappingStrategy.ByName)] public partial E1 ToE1(E2 source);",
            "public enum E1 {A}",
            "public enum E2 {A}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E2.A => global::E1.A,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E2 is not supported"),
                };
                """
            )
            .HaveDiagnostic(
                new(
                    DiagnosticDescriptors.TargetEnumValueDoesNotMatchTargetEnumType,
                    "Enum member E2.A (0) on E2 does not match type of target enum E1"
                )
            );
    }

    [Fact]
    public void MapEnumValueEnumTypeSourceMismatch()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnumValue(E1.A, E1.A), MapEnum(EnumMappingStrategy.ByName)] public partial E1 ToE1(E2 source);",
            "public enum E1 {A}",
            "public enum E2 {A}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E2.A => global::E1.A,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E2 is not supported"),
                };
                """
            )
            .HaveDiagnostic(
                new(
                    DiagnosticDescriptors.SourceEnumValueDoesNotMatchSourceEnumType,
                    "Enum member E1.A (0) on E1 does not match type of source enum E2"
                )
            );
    }

    [Fact]
    public void EnumToOtherEnumShouldCast()
    {
        var source = TestSourceBuilder.Mapping("E2", "E1", "enum E1 {A, B, C}", "enum E2 {A, B, C}");
        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return (global::E1)source;");
    }

    [Fact]
    public void EnumToSameEnumShouldAssign()
    {
        var source = TestSourceBuilder.Mapping("E1", "E1", "enum E1 {A, B, C}");

        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return source;");
    }

    [Fact]
    public void EnumToOtherEnumTypeShouldCast()
    {
        var source = TestSourceBuilder.Mapping("E1", "E2", "enum E1 : short {A, B, C}", "enum E2 : byte {A, B, C}");
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowInfoDiagnostics)
            .Should()
            .HaveSingleMethodBody("return (global::E2)source;");
    }

    [Fact]
    public void CustomClassToEnumWithBaseTypeCastShouldCast()
    {
        var source = TestSourceBuilder.Mapping(
            "C",
            "E",
            "class C { public static explicit operator byte(C c) => 0; } }",
            "enum E : byte {A, B, C}"
        );
        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return (global::E)(byte)source;");
    }

    [Fact]
    public void EnumToOtherEnumByValueShouldCast()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByValue)] partial E2 ToE1(E1 source);",
            "enum E1 {A, B, C}",
            "enum E2 {A = 100, B, C}"
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowInfoDiagnostics)
            .Should()
            .HaveSingleMethodBody("return (global::E2)source;");
    }

    [Fact]
    public void EnumToOtherEnumByNameShouldSwitch()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByName, IgnoreCase = false)] partial E2 ToE1(E1 source);",
            "enum E1 {A, B, C, D, E}",
            "enum E2 {A = 100, B, C, d, e, E}"
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowInfoDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E1.A => global::E2.A,
                    global::E1.B => global::E2.B,
                    global::E1.C => global::E2.C,
                    global::E1.E => global::E2.E,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E1 is not supported"),
                };
                """
            );
    }

    [Fact]
    public void EnumToOtherEnumByNameIgnoreCaseShouldSwitch()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByName, IgnoreCase = true)] partial E2 ToE1(E1 source);",
            "enum E1 {A, B, C, D, E, f, F}",
            "enum E2 {A = 100, B, C, d, e, E, f}"
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowInfoDiagnostics)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E1.A => global::E2.A,
                    global::E1.B => global::E2.B,
                    global::E1.C => global::E2.C,
                    global::E1.D => global::E2.d,
                    global::E1.E => global::E2.E,
                    global::E1.f => global::E2.f,
                    global::E1.F => global::E2.f,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E1 is not supported"),
                };
                """
            );
    }

    [Fact]
    public Task EnumToOtherEnumByNameWithoutOverlap()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByName)] partial E2 ToE1(E1 source);",
            "enum E1 {A, B, C}",
            "enum E2 {D, E, F}"
        );

        return TestHelper.VerifyGenerator(source);
    }

    [Fact]
    public void EnumToOtherEnumByNameViaGlobalConfigShouldSwitch()
    {
        var source =
            @"
using System;
using System.Collections.Generic;
using Riok.Mapperly.Abstractions;

[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName)]
public partial class Mapper
{
    partial global::E2 ToE1(global::E1 source);
}

enum E1 {A, B, C}

enum E2 {A = 100, B, C}
";

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E1.A => global::E2.A,
                    global::E1.B => global::E2.B,
                    global::E1.C => global::E2.C,
                    _ => throw new System.ArgumentOutOfRangeException(nameof(source), source, "The value of enum E1 is not supported"),
                };
                """
            );
    }

    [Fact]
    public void NullableEnumToOtherEnumShouldCastWithNullHandling()
    {
        var source = TestSourceBuilder.Mapping("E1?", "E2", "enum E1 {A, B, C}", "enum E2 {A, B, C}");

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                "return source == null ? throw new System.ArgumentNullException(nameof(source)) : (global::E2)source.Value;"
            );
    }

    [Fact]
    public void NullableEnumToOtherNullableEnumShouldCast()
    {
        var source = TestSourceBuilder.Mapping("E1?", "E2?", "enum E1 {A, B, C}", "enum E2 {A, B, C}");

        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return source == null ? default : (global::E2)source.Value;");
    }

    [Fact]
    public void EnumToOtherNullableEnumShouldCast()
    {
        var source = TestSourceBuilder.Mapping("E1", "E2?", "enum E1 {A, B, C}", "enum E2 {A, B, C}");
        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return (global::E2? )(global::E2)source;");
    }

    [Fact]
    public void EnumToStringShouldSwitch()
    {
        var source = TestSourceBuilder.Mapping("E1", "string", "enum E1 {A, B, C}");
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    global::E1.A => nameof(global::E1.A),
                    global::E1.B => nameof(global::E1.B),
                    global::E1.C => nameof(global::E1.C),
                    _ => source.ToString(),
                };
                """
            );
    }

    [Fact]
    public void StringToEnumShouldSwitchIgnoreCase()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByName, IgnoreCase = true)] partial E1 ToE1(string source);",
            "enum E1 {A, B, C}"
        );
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    { } s when s.Equals(nameof(global::E1.A), System.StringComparison.OrdinalIgnoreCase) => global::E1.A,
                    { } s when s.Equals(nameof(global::E1.B), System.StringComparison.OrdinalIgnoreCase) => global::E1.B,
                    { } s when s.Equals(nameof(global::E1.C), System.StringComparison.OrdinalIgnoreCase) => global::E1.C,
                    _ => System.Enum.Parse<global::E1>(source, true),
                };
                """
            );
    }

    [Fact]
    public void StringToEnumShouldSwitch()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByName)] partial E1 ToE1(string source);",
            "enum E1 {A, B, C}"
        );
        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                return source switch
                {
                    nameof(global::E1.A) => global::E1.A,
                    nameof(global::E1.B) => global::E1.B,
                    nameof(global::E1.C) => global::E1.C,
                    _ => System.Enum.Parse<global::E1>(source, false),
                };
                """
            );
    }

    [Fact]
    public void EnumToEnumMappingAndExplicitCastMappingDisabledShouldDiagnostic()
    {
        var source = TestSourceBuilder.Mapping(
            "E2",
            "E1",
            TestSourceBuilderOptions.WithDisabledMappingConversion(MappingConversionType.EnumToEnum, MappingConversionType.ExplicitCast),
            "enum E1 {A, B, C}",
            "enum E2 {A, B, C}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(new(DiagnosticDescriptors.CouldNotCreateMapping));
    }

    [Fact]
    public void EnumToEnumMappingDisabledShouldUseExplicitCastMapping()
    {
        var source = TestSourceBuilder.Mapping(
            "E2",
            "E1",
            TestSourceBuilderOptions.WithDisabledMappingConversion(MappingConversionType.EnumToEnum),
            "enum E1 {A, B, C}",
            "enum E2 {A, B, C}"
        );
        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return (global::E1)source;");
    }

    [Fact]
    public void StringToEnumMappingAndParseMappingDisabledShouldDiagnostic()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByName)] partial E1 ToE1(string source);",
            TestSourceBuilderOptions.WithDisabledMappingConversion(MappingConversionType.StringToEnum, MappingConversionType.ParseMethod),
            "enum E1 {A, B, C}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(new(DiagnosticDescriptors.CouldNotCreateMapping));
    }

    [Fact]
    public void StringToEnumMappingDisabledShouldUseParseMethodMapping()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapEnum(EnumMappingStrategy.ByName)] partial E1 ToE1(string source);",
            TestSourceBuilderOptions.WithDisabledMappingConversion(MappingConversionType.StringToEnum),
            "enum E1 {A, B, C}"
        );
        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return (global::E1)int.Parse(source);");
    }

    [Fact]
    public void EnumToStringMappingAndToStringMethodMappingDisabledShouldDiagnostic()
    {
        var source = TestSourceBuilder.Mapping(
            "E1",
            "string",
            TestSourceBuilderOptions.WithDisabledMappingConversion(
                MappingConversionType.EnumToString,
                MappingConversionType.ToStringMethod
            ),
            "enum E1 {A, B, C}"
        );
        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(new(DiagnosticDescriptors.CouldNotCreateMapping));
    }

    [Fact]
    public void EnumToStringMappingDisabledShouldUseToStringMapping()
    {
        var source = TestSourceBuilder.Mapping(
            "E1",
            "string",
            TestSourceBuilderOptions.WithDisabledMappingConversion(MappingConversionType.EnumToString),
            "enum E1 {A, B, C}"
        );
        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return (string)source.ToString();");
    }

    [Theory]
    [InlineData("ByNameEqual", EnumMappingStrategy.ByName, "Value1, Value2", "Value1, Value2")]
    [InlineData("ByNameMissingSource", EnumMappingStrategy.ByName, "Value1", "Value1, Value2")]
    [InlineData("ByNameMissingTarget", EnumMappingStrategy.ByName, "Value1, Value2", "Value1")]
    [InlineData("ByNameMissingMultipleTarget", EnumMappingStrategy.ByName, "Value1, Value2, Value3", "Value1")]
    [InlineData("ByNameMissingMultipleSource", EnumMappingStrategy.ByName, "Value1", "Value1, Value2, Value3")]
    [InlineData("ByValueEqual", EnumMappingStrategy.ByValue, "Value1, Value2", "Value1, Value2")]
    [InlineData("ByValueMissingSource", EnumMappingStrategy.ByValue, "Value1", "Value1, Value2")]
    [InlineData("ByValueMissingTarget", EnumMappingStrategy.ByValue, "Value1, Value2", "Value1")]
    [InlineData("ByValueMissingMultipleTarget", EnumMappingStrategy.ByValue, "Value1, Value2, Value3", "Value1")]
    [InlineData("ByValueMissingMultipleSource", EnumMappingStrategy.ByValue, "Value1", "Value1, Value2, Value3")]
    public Task EnumToAnotherEnumByStrategyMissingValues(
        string testCase,
        EnumMappingStrategy enumMappingStrategy,
        string sourceEnumValues,
        string targetEnumValues
    )
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            TestSourceBuilderOptions.Default with
            {
                EnumMappingStrategy = enumMappingStrategy
            },
            "class A { public C Value { get; set; } }",
            "class B { public D Value { get; set; } }",
            $"enum C {{ {sourceEnumValues} }}",
            $"enum D {{ {targetEnumValues} }}"
        );

        return TestHelper.VerifyGenerator(source, null, testCase);
    }

    [Theory]
    [InlineData("SourceLower", "value1", "Value1")]
    [InlineData("SourceLowerMissingSource", "value2", "Value2, Value3")]
    [InlineData("TargetLower", "Value3", "value3")]
    [InlineData("TargetLowerMissingTarget", "Value4, Value5", "value4")]
    [InlineData("TargetLowerMissingMultipleTarget", "Value5, Value6, Value7", "value6")]
    [InlineData("TargetLowerMissingMultipleSource", "Value6", "value5, value6, value7")]
    public Task EnumToAnotherEnumByNameCaseInsensitive(string testCase, string sourceEnumValues, string targetEnumValues)
    {
        var source = TestSourceBuilder.Mapping(
            "A",
            "B",
            TestSourceBuilderOptions.Default with
            {
                EnumMappingStrategy = EnumMappingStrategy.ByName,
                EnumMappingIgnoreCase = true
            },
            "class A { public C Value { get; set; } }",
            "class B { public D Value { get; set; } }",
            $"enum C {{ {sourceEnumValues} }}",
            $"enum D {{ {targetEnumValues} }}"
        );

        return TestHelper.VerifyGenerator(source, null, testCase);
    }
}
