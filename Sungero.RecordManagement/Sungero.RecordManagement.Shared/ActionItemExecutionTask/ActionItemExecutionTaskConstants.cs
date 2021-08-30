namespace Sungero.RecordManagement.Constants
{
  public static class ActionItemExecutionTask
  {
    public const string WorkingWithGUI = "Working with GUI";
    
    [Sungero.Core.Public]
    public const int MaxCompoundGroup = 100;
    
    public const int MaxActionItemAssignee = 250;
    
    [Sungero.Core.Public]
    public const string CheckDeadline = "CheckDeadline";
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задачи на исполнение поручения.
    /// </summary>
    public const string ActionItemExecutionTaskConfirmDialogID = "1116c237-c585-4186-a7e1-ba81db27e420";
    
    /// <summary>
    /// ИД диалога подтверждения при выполнении задания на исполнение поручения.
    /// </summary>
    public const string ActionItemExecutionAssignmentConfirmDialogID = "bd3560c8-dee0-4fce-a17f-005b170e7835";
    
    /// <summary>
    /// ИД диалогов подтверждения при выполнении задания на приемку работ контролером.
    /// </summary>
    public static class ActionItemSupervisorAssignmentConfirmDialogID
    {
      /// <summary>
      /// С результатом "Принято".
      /// </summary>
      public const string Agree = "a70db196-fe4d-4d49-a0aa-b75821b9a03f";
      
      /// <summary>
      /// С результатом "На доработку".
      /// </summary>
      public const string ForRework = "f27aad24-144e-4326-ad3c-30034c0c6f56";
    }
  }
}