import AuthShell from '../_components/AuthShell';
import ResetForm from './ResetForm';

export default function ForgotPasswordPage() {
  return (
    <AuthShell title="Reset your password">
      <ResetForm />
    </AuthShell>
  );
}
