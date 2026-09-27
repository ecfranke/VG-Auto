import AuthShell from '../_components/AuthShell';
import { getProviders } from '../_lib';
import LoginForm from './LoginForm';
import { LoginState } from './authenticate';

export default async function LoginPage({ searchParams }: { searchParams: Promise<Record<string, string | undefined>> }) {
  const params = await searchParams;
  const providers = await getProviders();

  // a Microsoft sign in that still needs the emailed code comes back here with the challenge
  const initialState: LoginState = params.challenge
    ? { step: 'code', challengeId: params.challenge, emailHint: params.hint ?? null, info: params.info }
    : { step: 'password', error: params.error, info: params.info };

  return (
    <AuthShell title="Sign in to your account">
      <LoginForm initialState={initialState} passwordReset={providers.passwordReset} microsoft={providers.microsoft.enabled} />
    </AuthShell>
  );
}
