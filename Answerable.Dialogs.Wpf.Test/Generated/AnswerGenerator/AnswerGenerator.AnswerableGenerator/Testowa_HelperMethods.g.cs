namespace Answerable.Dialogs.Wpf.Test 
{
    public partial class Testowa 
{
  
        private async System.Threading.Tasks.Task<Answers.Answer> TryAsync(
       System.Func<System.Threading.Tasks.Task<Answers.Answer>> method,
       System.Threading.CancellationToken ct,
       [System.Runtime.CompilerServices.CallerMemberName] System.String callerName = "",
       [System.Runtime.CompilerServices.CallerFilePath] System.String callerFilePath = "",
       [System.Runtime.CompilerServices.CallerLineNumber] System.Int32 callerLineNumber = 0)
        {

            var timeoutValue = _answerService.HasTimeout ? _answerService.GetTimeout() : System.TimeSpan.Zero; // Pobiera i resetuje timeout
            System.Threading.Tasks.Task<Answers.Answer> methodTask = method();
            // repeat until method returns a successful answer or dialog is concluded
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            stopwatch.Start();
            while (true)
            {
                //AnswerService has timeout set, so we need to wait for the method to complete or timeout to occur
                if (timeoutValue != System.TimeSpan.Zero)
                {
                    Answers.Answer answer;
                    try
                    {
                        answer = await methodTask.WaitAsync(timeoutValue, ct);
                    }
                    catch (System.TimeoutException)
                    {
                        string warningMessage = string.Format(
                            _answerService.Strings.WarningMessageFormat,
                            callerName,
                            System.IO.Path.GetFileName(callerFilePath),
                            callerLineNumber,
                            "Operation timed out"
                        );
                        _answerService.LogWarning(warningMessage);

                        System.String action = string.Format(
                            _answerService.Strings.CallerMessageFormat,
                            callerName,
                            System.IO.Path.GetFileName(callerFilePath),
                            callerLineNumber
                        );
                        // if timeout dialogs are implemented
                        if (_answerService.HasTimeOutDialog || _answerService.HasTimeOutAsyncDialog)
                        {
                            System.String timeoutMessage = string.Format(_answerService.Strings.TimeoutMessage, action);
                            // async dialog has priority, but sync will run if async is not available
                            using var dialogCts = new System.Threading.CancellationTokenSource();
                            using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ct, dialogCts.Token);
                            System.Threading.Tasks.Task<bool> dialogTask = ChooseBetweenAsyncAndNonAsyncDialogTask(timeoutMessage, linkedCts.Token);
                            var response = await ProcessTimeOutDialog(dialogTask, dialogCts);

                            switch (response.Response)
                            {
                                case Answers.Dialogs.DialogResponse.Continue:
                                    // carry on waiting
                                    continue;
                                case Answers.Dialogs.DialogResponse.Cancel:
                                    stopwatch.Stop();
                                    return response.Answer;
                                case Answers.Dialogs.DialogResponse.DoNotWait:
                                    stopwatch.Stop();
                                    return response.Answer;
                                case Answers.Dialogs.DialogResponse.Answered:
                                    if (response.Answer.IsSuccess)
                                    {
                                        stopwatch.Stop();
                                        return response.Answer;
                                    }
                                    methodTask = method();
                                    continue;
                            }

                        }
                        // Użytkownik wybrał "No" lub brak dostępnych dialogów
                        return TimedOutResponse();
                    }
                    catch (System.OperationCanceledException)
                    {
                        return Answers.Answer.Prepare(_answerService.Strings.CancelledText).Error(_answerService.Strings.CancelMessage);
                    }

                    var responseReceivedWithinTimeout = await ProcessAnswerAsync(answer);
                    switch (responseReceivedWithinTimeout.Response)
                    {
                        case Answers.Dialogs.DialogResponse.Answered:
                            stopwatch.Stop();
                            return responseReceivedWithinTimeout.Answer;
                        case Answers.Dialogs.DialogResponse.DoNotRepeat:
                            stopwatch.Stop();
                            return responseReceivedWithinTimeout.Answer;
                        case Answers.Dialogs.DialogResponse.Continue:
                            continue;
                    }
                }
                // Brak określonego timeoutu

                var noTimeoutSetResponse = await ProcessAnswerAsync(await methodTask);
                switch (noTimeoutSetResponse.Response)
                {
                    case Answers.Dialogs.DialogResponse.Answered:
                        stopwatch.Stop();
                        return noTimeoutSetResponse.Answer;
                    case Answers.Dialogs.DialogResponse.DoNotRepeat:
                        stopwatch.Stop();
                        return noTimeoutSetResponse.Answer;
                    case Answers.Dialogs.DialogResponse.Continue:
                        continue;
                }
            }


            Answers.Answer TimedOutResponse() => Answers.Answer.Prepare(_answerService.Strings.TimeOutText).Error(string.Format(_answerService.Strings.TimeoutElapsedMessage, stopwatch.Elapsed.TotalSeconds));

            System.Threading.Tasks.Task<bool> ChooseBetweenAsyncAndNonAsyncDialogTask(string s, System.Threading.CancellationToken linkedCts) =>
             _answerService.HasTimeOutAsyncDialog ? _answerService.AskYesNoToWaitAsync(s, linkedCts) :
                    System.Threading.Tasks.Task.Run(() =>
                        _answerService.AskYesNoToWait(s, linkedCts), ct);


            async System.Threading.Tasks.Task<(Answers.Dialogs.DialogResponse Response, Answers.Answer Answer)> ProcessAnswerAsync(Answers.Answer localAnswer)
            {
                if (!localAnswer.IsSuccess)
                {
                    string errorMessage = string.Format(
                        _answerService.Strings.ErrorMessageFormat,
                        callerName,
                        System.IO.Path.GetFileName(callerFilePath),
                        callerLineNumber,
                        localAnswer.Message
                    );
                    _answerService.LogError(errorMessage);
                }
                if (localAnswer.IsSuccess || localAnswer.DialogConcluded || !(_answerService.HasYesNoDialog || _answerService.HasYesNoAsyncDialog))
                {
                    return (Answers.Dialogs.DialogResponse.Answered, localAnswer);
                }

                System.Boolean userResponse = _answerService.HasYesNoAsyncDialog ? await _answerService.AskYesNoAsync(localAnswer.Message, ct) :
                    _answerService.AskYesNo(localAnswer.Message);

                if (userResponse)
                {
                    methodTask = method();
                    return (Answers.Dialogs.DialogResponse.Continue, null);
                }

                localAnswer.ConcludeDialog();
                string userCancelledMessage = string.Format(
                    _answerService.Strings.UserCancelledMessageFormat,
                    callerName,
                    System.IO.Path.GetFileName(callerFilePath),
                    callerLineNumber
                );
                _answerService.LogError(userCancelledMessage);
                return (Answers.Dialogs.DialogResponse.DoNotRepeat, localAnswer); // Użytkownik wybrał "No", kończymy
            }


            async System.Threading.Tasks.Task<(Answers.Dialogs.DialogResponse Response, Answers.Answer Answer)> ProcessTimeOutDialog(
                System.Threading.Tasks.Task<bool> dialogTask,
                System.Threading.CancellationTokenSource dialogCts)
            {
                try
                {
                    var dialogOutcomeTask = await System.Threading.Tasks.Task.WhenAny(methodTask, dialogTask).WaitAsync(ct);

                    if (dialogOutcomeTask == methodTask)
                    {
                        var localAnswer = await methodTask;
                        await dialogCts.CancelAsync();
                        return (Answers.Dialogs.DialogResponse.Answered, localAnswer);
                    }

                    // Sprawdzamy czy dialog został zakończony przez użytkownika
                    if (await dialogTask)
                    {
                        return (Answers.Dialogs.DialogResponse.Continue, null);
                    }
                    string userCancelledMessage = string.Format(
                        _answerService.Strings.UserCancelledMessageFormat,
                        callerName,
                        System.IO.Path.GetFileName(callerFilePath),
                        callerLineNumber
                    );
                    _answerService.LogError(userCancelledMessage);
                    return (Answers.Dialogs.DialogResponse.DoNotWait,
                        Answers.Answer.Prepare(_answerService.Strings.CancelledText).Error(_answerService.Strings.TimeoutError).ConcludeDialog());
                }
                catch (System.OperationCanceledException)
                {
                    string errorMessage = string.Format(
                        _answerService.Strings.UserCancelledMessageFormat,
                        callerName,
                        System.IO.Path.GetFileName(callerFilePath),
                        callerLineNumber
                    );
                    _answerService.LogError(errorMessage);
                    return (Answers.Dialogs.DialogResponse.Cancel,
                        Answers.Answer.Prepare(_answerService.Strings.CancelMessage).Error(_answerService.Strings.CancelMessage).ConcludeDialog());
                }
            }
        }

 }
}