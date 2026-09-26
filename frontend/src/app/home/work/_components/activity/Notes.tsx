'use client';

import FormTextArea from "@/_components/FormTextArea";
import { SecondaryText } from "@/_components/SecondaryText"; 

export default function ActivityNotes({
  notes,
  edit
}: {
  notes: string,
  edit:boolean
}) {
    return (
        !edit ? <SecondaryText>{notes}</SecondaryText> :
        <FormTextArea placeholder={'Add notes ...'} defaultValue={notes} name="notes"></FormTextArea>
    );
}