namespace Clean.Architecture.Core.Domain.TestAggregate;
public class TestJSON
{
  public Test1Props? one { get; set; }
  public Test2Props? two { get; set; }
}

public class Test1Props
{
  public string prop1 { get; set; }
  public string prop2 { get; set; }
}

public class Test2Props
{
  public string prop_2_1 { get; set; }
  public string prop_2_2 { get; set; }
}
