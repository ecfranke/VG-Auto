
'use server'

import SettingsTabs from '@/_components/SettingsTabs'
import Main from '../../_components/Main'
import { httpGet } from '@/_lib/server/query-api';
import FormInput from '@/_components/FormInput';
import { changePassword, createOrUpdate, unlinkExternalLogin } from '../createOrUpdate';
import { IUserProfile } from '../model'; 
import ProfileImage from './ProfileImage';



export default async function Page() {

  const data = await httpGet('profile');
  const options = await data.json() as IUserProfile;
  const linked = await (await httpGet('profile/externallogins')).json() as { provider: string, email: string, createdAt: string }[];
  
   
  return (

    <Main header={
      <SettingsTabs>
      </SettingsTabs>
    } narrow={true}>

      <div className="space-y-12">
      <div className="border-b border-gray-900/10 pb-12">
            <h2 className="text-base/7 font-semibold text-gray-900   my-4" >Personal Information</h2>
            <form  action={createOrUpdate}>
              
            <div className="  grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">
              <ProfileImage options={options}></ProfileImage>
              <div className="sm:col-span-3">
                <FormInput name='firstName' label='First name' defaultValue={options.firstName}></FormInput>
              </div>
              <div className="sm:col-span-3">
                <FormInput name='lastName' label='Last name' defaultValue={options.lastName}></FormInput>
              </div>
              <div className="sm:col-span-full">
                <FormInput name='email' label='Email' defaultValue={options.email}></FormInput>
              </div>
              <div className="sm:col-span-full">
                <FormInput name='userName' label='Username' defaultValue={options.userName}></FormInput>
              </div>
              <div className="sm:col-span-full flex items-center justify-end gap-x-6">

                <button
                  type="submit"
                  className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
                >
                  Save
                </button>
              </div>
            </div>
            </form>
          </div>
       
        <div className="border-b border-gray-900/10 pb-12">
          <h2 className="text-base/7 font-semibold text-gray-900">Change password</h2>
          <form   action={changePassword}>
          <div className="mt-10 grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">
            <div className="sm:col-span-full">
              <FormInput name='currentPassword' type='password' label='Current password' ></FormInput>
            </div>
            <div className="sm:col-span-full">
              <FormInput name='newPassword' type='password' label='New password' ></FormInput>
            </div>
            <div className="sm:col-span-full">
              <FormInput name='confirmPassword' type='password' label='Confirm password' ></FormInput>
            </div>
            <div className="sm:col-span-full flex items-center justify-end gap-x-6">

              <button
                type="submit"
                className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600"
              >
                Save
              </button>
            </div>
          </div>
          </form>
        </div>
        <div className="border-b border-gray-900/10 pb-12">
          <h2 className="text-base/7 font-semibold text-gray-900">Microsoft account</h2>
          {linked.length === 0 ? (
            <p className="mt-1 text-sm/6 text-gray-500">No Microsoft account is linked. Use &quot;Sign in with Microsoft&quot; on the login page; the first time you confirm it with a code sent to your email address.</p>
          ) : linked.map((l) => (
            <form key={l.provider} action={unlinkExternalLogin} className="mt-4 flex items-center justify-between gap-x-6">
              <input type="hidden" name="provider" value={l.provider} />
              <p className="text-sm/6 text-gray-700">Linked: <span className="font-semibold">{l.email}</span></p>
              <button type="submit" className="rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 shadow-xs ring-1 ring-gray-300 ring-inset hover:bg-gray-50">Unlink</button>
            </form>
          ))}
        </div>
        <div className="border-b border-gray-900/10 pb-12">
          <h2 className="text-base/7 font-semibold text-gray-900">Delete account</h2>
          <p className="mt-1 text-sm/6 text-gray-400">No longer want to use our service? You can delete your account here. This action is not reversible. All information related to this account will be deleted permanently.</p>

          <div className="mt-10 grid grid-cols-1 gap-x-6 gap-y-8 sm:grid-cols-6">
            <div className="sm:col-span-full flex items-center justify-end gap-x-6">
              <button
                type="submit"
                className="rounded-md bg-red-500 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-red-400"
              >
                Yes, delete my account
              </button>
            </div>
          </div>
        </div>
      </div>

    </Main>
  )

}



