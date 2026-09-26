'use client'

import { FormRadio } from "@/_components/FormInput"
import FormSwitch from "@/_components/FormSwitch";

export default function SearchStatusFilter({
  status,
  issued,
}: {
  status?: string | undefined
  issued: boolean
}) {
  function submitFormOnChange(event: React.ChangeEvent<HTMLInputElement>): void {
    event.currentTarget.form?.submit();
  }

  return (
    <div className=" flex gap-x-2 mb-2">
      <div className="flex  items-center gap-x-2 ">
        <input type="hidden" id="issued" name="issued" value={(issued ? "on" : "off")}></input>
        <FormSwitch
          defaultChecked={issued}
          small={true}
          onChange={() => {
            const issuedHidden = document.getElementById("issued");
            if (issuedHidden) (issuedHidden as HTMLInputElement).value = issued ? "off" : "on";
            //reset radio
            const radioAll = document.getElementById('all') as HTMLInputElement;
            if (radioAll.checked) document.getElementById('btnSubmit')?.click()
            else radioAll?.click();
          }} >
        </FormSwitch>
        <label className="block text-sm/6 font-medium text-gray-900">{(issued ? 'Completed' : 'Unfinished')}</label>
      </div>
      <div className="flex   items-center  gap-x-2 ">
        <FormRadio id="all" label="All" name="status" onChange={submitFormOnChange} defaultChecked={(!status || status === 'all')} value="all" ></FormRadio>
      </div>
      {!issued && <div className="flex  items-center  gap-x-2 ">
        <FormRadio id="inprogress" label="In progress" name="status" onChange={submitFormOnChange} defaultChecked={(status === 'inprogress')} value="inprogress" ></FormRadio>
      </div>}
      {!issued && <div className="flex   items-center  gap-x-2 ">
        <FormRadio id="closed" label="Closed" name="status" onChange={submitFormOnChange} defaultChecked={(status === 'closed')} value="closed" ></FormRadio>
      </div>}
      {issued && <div className="flex   items-center  gap-x-2 ">
        <FormRadio id="overdue" label="Invoice overdue" name="status" onChange={submitFormOnChange} defaultChecked={(status === 'overdue')} value="overdue" ></FormRadio>
      </div>}
    </div>
  )
}