'use server'
import { authApi, authErrorMessage } from '../_lib';

export interface ResetState {
  step: 'request' | 'reset' | 'done',
  challengeId?: string,
  error?: string,
}

export async function resetPassword(prevState: ResetState, formData: FormData): Promise<ResetState> {
  if (prevState.step === 'request') {
    const result = await authApi('password/forgot', { login: formData.get('login')?.toString() ?? '' });
    if (result.codeRequired && result.challengeId) return { step: 'reset', challengeId: result.challengeId };
    return { step: 'request', error: authErrorMessage(result, 'Password reset is not available.') };
  }

  const newPassword = formData.get('newPassword')?.toString() ?? '';
  if (newPassword !== formData.get('confirmPassword')?.toString()) {
    return { ...prevState, error: 'The passwords do not match.' };
  }
  const result = await authApi('password/reset', {
    challengeId: prevState.challengeId,
    code: formData.get('code')?.toString() ?? '',
    newPassword,
  });
  if (result.ok) return { step: 'done' };
  return { ...prevState, error: authErrorMessage(result, 'Password reset failed.') };
}
